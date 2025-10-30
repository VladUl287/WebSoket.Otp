using System.Diagnostics;

namespace WebSockets.Otp.Core.Helpers;

public static class ParallelState
{
    public static Task ForEachAsync<TSource, TState>(IAsyncEnumerable<TSource> source, int dop, TaskScheduler scheduler,
        CancellationToken cancellationToken, Func<TSource, TState, CancellationToken, ValueTask> body, TState state)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            var syncState = new AsyncForEachAsyncState<TSource, TState>(source, TaskBody<TSource, TState>, dop, 
                scheduler, cancellationToken, body, state);
            syncState.TryRunWorker();
            return syncState.Task;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    private static async Task TaskBody<TSource, TState>(object o)
    {
        var state = (AsyncForEachAsyncState<TSource, TState>)o;
        bool launchedNext = false;

        try
        {
            while (!state.Cancellation.IsCancellationRequested)
            {
                TSource element;
                await state.AcquireLock();
                try
                {
                    if (state.Cancellation.IsCancellationRequested || !await state.Enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    element = state.Enumerator.Current;
                }
                finally
                {
                    state.ReleaseLock();
                }

                if (!launchedNext)
                {
                    launchedNext = true;
                    state.TryRunWorker();
                }

                await state.LoopBody(element, state.State, state.Cancellation.Token);
            }
        }
        catch (Exception e)
        {
            state.RecordException(e);
        }
        finally
        {
            if (state.SignalWorkerCompletedIterating())
            {
                try
                {
                    await state.DisposeAsync();
                }
                catch (Exception e)
                {
                    state.RecordException(e);
                }

                state.Complete();
            }
        }
    }

    private sealed class AsyncForEachAsyncState<TSource, TState> : ForEachAsyncState<TSource, TState>, IAsyncDisposable
    {
        public readonly IAsyncEnumerator<TSource> Enumerator;
        public TState State;

        public AsyncForEachAsyncState(
            IAsyncEnumerable<TSource> source, Func<object, Task> taskBody,
            int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
            Func<TSource, TState, CancellationToken, ValueTask> body, TState state) :
            base(taskBody, needsLock: true, dop, scheduler, cancellationToken, body)
        {
            Enumerator = source.GetAsyncEnumerator(Cancellation.Token) ?? throw new InvalidOperationException();
            State = state;
        }

        public ValueTask DisposeAsync()
        {
            _registration.Dispose();
            return Enumerator.DisposeAsync();
        }
    }

    private abstract class ForEachAsyncState<TSource, TState> : TaskCompletionSource, IThreadPoolWorkItem
    {
        /// <summary>The caller-provided cancellation token.</summary>
        private readonly CancellationToken _externalCancellationToken;
        /// <summary>Registration with caller-provided cancellation token.</summary>
        protected readonly CancellationTokenRegistration _registration;
        /// <summary>
        /// The delegate to invoke on each worker to run the enumerator processing loop.
        /// </summary>
        /// <remarks>
        /// This could have been an action rather than a func, but it returns a task so that the task body is an async Task
        /// method rather than async void, even though the worker body catches all exceptions and the returned Task is ignored.
        /// </remarks>
        private readonly Func<object, Task> _taskBody;
        /// <summary>The <see cref="TaskScheduler"/> on which all work should be performed.</summary>
        private readonly TaskScheduler _scheduler;
        /// <summary>The <see cref="ExecutionContext"/> present at the time of the ForEachAsync invocation.  This is only used if on the default scheduler.</summary>
        private readonly ExecutionContext? _executionContext;
        /// <summary>Semaphore used to provide exclusive access to the enumerator.</summary>
        private readonly SemaphoreSlim? _lock;

        /// <summary>The number of outstanding workers.  When this hits 0, the operation has completed.</summary>
        private int _completionRefCount;
        /// <summary>Any exceptions incurred during execution.</summary>
        private List<Exception>? _exceptions;
        /// <summary>The number of workers that may still be created.</summary>
        private int _remainingDop;

        /// <summary>The delegate to invoke for each element yielded by the enumerator.</summary>
        public readonly Func<TSource, TState, CancellationToken, ValueTask> LoopBody;
        /// <summary>The internal token source used to cancel pending work.</summary>
        public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

        /// <summary>Initializes the state object.</summary>
        protected ForEachAsyncState(Func<object, Task> taskBody, bool needsLock, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, TState, CancellationToken, ValueTask> body)
        {
            _taskBody = taskBody;
            _lock = needsLock ? new SemaphoreSlim(initialCount: 1, maxCount: 1) : null;
            _remainingDop = dop < 0 ? 20 : dop;
            LoopBody = body;
            _scheduler = scheduler;
            if (scheduler == TaskScheduler.Default)
            {
                _executionContext = ExecutionContext.Capture();
            }

            _externalCancellationToken = cancellationToken;
            _registration = cancellationToken.UnsafeRegister(static o => ((ForEachAsyncState<TSource, TState>)o!).Cancellation.Cancel(), this);
        }

        /// <summary>Queues another worker if allowed by the remaining degree of parallelism permitted.</summary>
        /// <remarks>This is not thread-safe and must only be invoked by one worker at a time.</remarks>
        public void TryRunWorker()
        {
            if (_remainingDop > 0)
            {
                _remainingDop--;

                // Queue the invocation of the worker/task body.  Note that we explicitly do not pass a cancellation token here,
                // as the task body is what's responsible for completing the ForEachAsync task, for decrementing the reference count
                // on pending tasks, and for cleaning up state.  If a token were passed to StartNew (which simply serves to stop the
                // task from starting to execute if it hasn't yet by the time cancellation is requested), all of that logic could be
                // skipped, and bad things could ensue, e.g. deadlocks, leaks, etc.  Also note that we need to increment the pending
                // work item ref count prior to queueing the worker in order to avoid race conditions that could lead to temporarily
                // and erroneously bouncing at zero, which would trigger completion too early.
                Interlocked.Increment(ref _completionRefCount);
                if (_scheduler == TaskScheduler.Default)
                {
                    // If the scheduler is the default, we can avoid the overhead of the StartNew Task by just queueing
                    // this state object as the work item.
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
                }
                else
                {
                    // We're targeting a non-default TaskScheduler, so queue the task body to it.
                    Task.Factory.StartNew(_taskBody!, this, default(CancellationToken), TaskCreationOptions.DenyChildAttach, _scheduler);
                }
            }
        }

        /// <summary>Signals that the worker has completed iterating.</summary>
        /// <returns>true if this is the last worker to complete iterating; otherwise, false.</returns>
        public bool SignalWorkerCompletedIterating() => Interlocked.Decrement(ref _completionRefCount) == 0;

        /// <summary>Asynchronously acquires exclusive access to the enumerator.</summary>
        public Task AcquireLock()
        {
            // We explicitly don't pass this.Cancellation to WaitAsync.  Doing so adds overhead, and it isn't actually
            // necessary. All of the operations that monitor the lock are part of the same ForEachAsync operation, and the Task
            // returned from ForEachAsync can't complete until all of the constituent operations have completed, including whoever
            // holds the lock while this worker is waiting on the lock.  Thus, the lock will need to be released for the overall
            // operation to complete.  Passing the token would allow the overall operation to potentially complete a bit faster in
            // the face of cancellation, in exchange for making it a bit slower / more overhead in the common case of cancellation
            // not being requested.  We want to optimize for the latter.  This also then avoids an exception throw / catch when
            // cancellation is requested.
            Debug.Assert(_lock is not null, "Should only be invoked when _lock is non-null");
            return _lock.WaitAsync(CancellationToken.None);
        }

        /// <summary>Relinquishes exclusive access to the enumerator.</summary>
        public void ReleaseLock()
        {
            Debug.Assert(_lock is not null, "Should only be invoked when _lock is non-null");
            _lock.Release();
        }

        /// <summary>Stores an exception and triggers cancellation in order to alert all workers to stop as soon as possible.</summary>
        /// <param name="e">The exception.</param>
        public void RecordException(Exception e)
        {
            // Store the exception.
            lock (this)
            {
                (_exceptions ??= new List<Exception>()).Add(e);
            }

            // Trigger cancellation of all workers.  If cancellation has already been triggered
            // due to a previous exception occurring, this is a nop.
            try
            {
                Cancellation.Cancel();
            }
            catch (AggregateException ae)
            {
                // If cancellation callbacks erroneously throw exceptions, include those exceptions in the list.
                lock (this)
                {
                    _exceptions.AddRange(ae.InnerExceptions);
                }
            }
        }

        /// <summary>Completes the ForEachAsync task based on the status of this state object.</summary>
        public void Complete()
        {
            Debug.Assert(_completionRefCount == 0, $"Expected {nameof(_completionRefCount)} == 0, got {_completionRefCount}");

            bool taskSet;
            if (_externalCancellationToken.IsCancellationRequested)
            {
                // The externally provided token had cancellation requested. Assume that any exceptions
                // then are due to that, and just cancel the resulting task.
                taskSet = TrySetCanceled(_externalCancellationToken);
            }
            else if (_exceptions is null)
            {
                // Everything completed successfully.
                Debug.Assert(!Cancellation.IsCancellationRequested);
                taskSet = TrySetResult();
            }
            else
            {
                // Fail the task with the resulting exceptions.  The first should be the initial
                // exception that triggered the operation to shut down.  The others, if any, may
                // include cancellation exceptions from other concurrent operations being canceled
                // in response to the primary exception.
                taskSet = TrySetException(_exceptions);
            }

            Debug.Assert(taskSet, "Complete should only be called once.");
        }

        /// <summary>Executes the task body using the <see cref="ExecutionContext"/> captured when ForEachAsync was invoked.</summary>
        void IThreadPoolWorkItem.Execute()
        {
            Debug.Assert(_scheduler == TaskScheduler.Default, $"Expected {nameof(_scheduler)} == TaskScheduler.Default, got {_scheduler}");

            if (_executionContext is null)
            {
                _taskBody(this);
            }
            else
            {
                ExecutionContext.Run(_executionContext, static o => ((ForEachAsyncState<TSource, TState>)o!)._taskBody(o), this);
            }
        }
    }
}

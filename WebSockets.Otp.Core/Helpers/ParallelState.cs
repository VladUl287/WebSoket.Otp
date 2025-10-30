using System.Collections.Concurrent;

namespace WebSockets.Otp.Core.Helpers;

public static class ParallelState
{
    public static Task ForEachStateAsync<TSource, TState>(
        IAsyncEnumerable<TSource> source, int dop, TaskScheduler scheduler,
        Func<TSource, TState, CancellationToken, ValueTask> body, TState state, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromCanceled(token);

        try
        {
            var syncState = new SyncForEachAsyncStateWithStateAsync<TSource, TState>(
                source, TaskBody<TSource, TState>, dop,
                scheduler, token, body, state);
            syncState.QueueWorkerIfDopAvailable();
            return syncState.Task;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }

    private static async Task TaskBody<TSource, TState>(object o)
    {
        var syncState = (SyncForEachAsyncStateWithStateAsync<TSource, TState>)o;
        var launchedNext = false;

        try
        {
            while (!syncState.Cancellation.IsCancellationRequested)
            {
                var (success, element) = await syncState.TryGetNextAsync();
                if (!success)
                    break;

                if (!launchedNext)
                {
                    launchedNext = true;
                    syncState.QueueWorkerIfDopAvailable();
                }

                await syncState.LoopBody(element, syncState.State, syncState.Cancellation.Token);
            }
        }
        catch (Exception e)
        {
            syncState.RecordException(e);
        }
        finally
        {
            if (syncState.SignalWorkerCompletedIterating())
            {
                try
                {
                    await syncState.DisposeAsync();
                }
                catch (Exception e)
                {
                    syncState.RecordException(e);
                }

                syncState.Complete();
            }
        }
    }

    private sealed class SyncForEachAsyncStateWithStateAsync<TSource, TState> : ForEachAsyncStateWithState<TSource, TState>, IAsyncDisposable
    {
        public readonly IAsyncEnumerator<TSource> Enumerator;
        public readonly TState State;

        public SyncForEachAsyncStateWithStateAsync(
            IAsyncEnumerable<TSource> source, Func<object, Task> taskBody,
            int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
            Func<TSource, TState, CancellationToken, ValueTask> body, TState state) :
                base(taskBody, dop, scheduler, cancellationToken, body)
        {
            Enumerator = source.GetAsyncEnumerator() ?? throw new InvalidOperationException();
            State = state;
        }

        public async ValueTask<(bool Success, TSource Element)> TryGetNextAsync()
        {
            await AcquireLock(); //TODO: spin wait + semaphore lock?
            try
            {
                if (Cancellation.IsCancellationRequested || !await Enumerator.MoveNextAsync())
                    return (false, default);

                return (true, Enumerator.Current);
            }
            finally
            {
                ReleaseLock();
            }
        }

        public ValueTask DisposeAsync()
        {
            _registration.Dispose();
            return Enumerator.DisposeAsync();
        }
    }

    private abstract class ForEachAsyncStateWithState<TSource, TState> : TaskCompletionSource, IThreadPoolWorkItem
    {
        private readonly CancellationToken _externalCancellationToken;

        protected readonly CancellationTokenRegistration _registration;

        private readonly Func<object, Task> _taskBody;

        private readonly TaskScheduler _scheduler;

        private readonly ExecutionContext? _executionContext;

        private readonly SemaphoreSlim _lock;

        private int _completionRefCount;

        private ConcurrentBag<Exception>? _exceptions;

        private int _remainingDop;

        public readonly Func<TSource, TState, CancellationToken, ValueTask> LoopBody;

        public readonly CancellationTokenSource Cancellation = new();

        protected ForEachAsyncStateWithState(Func<object, Task> taskBody, int dop,
            TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, TState, CancellationToken, ValueTask> body)
        {
            LoopBody = body;

            _taskBody = taskBody;
            _lock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
            _remainingDop = dop < 0 ? 1 : dop;
            _scheduler = scheduler;

            if (scheduler == TaskScheduler.Default)
            {
                _executionContext = ExecutionContext.Capture();
            }

            _externalCancellationToken = cancellationToken;
            _registration = cancellationToken.UnsafeRegister(static o => ((ForEachAsyncStateWithState<TSource, TState>)o!).Cancellation.Cancel(), this);
        }

        public void QueueWorkerIfDopAvailable()
        {
            if (_remainingDop > 0)
            {
                _remainingDop--;

                Interlocked.Increment(ref _completionRefCount);
                if (_scheduler == TaskScheduler.Default)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
                }
                else
                {
                    Task.Factory.StartNew(_taskBody!, this, default, TaskCreationOptions.DenyChildAttach, _scheduler);
                }
            }
        }

        public bool SignalWorkerCompletedIterating() => Interlocked.Decrement(ref _completionRefCount) == 0;

        public Task AcquireLock() => _lock.WaitAsync(CancellationToken.None);

        public void ReleaseLock() => _lock.Release();

        public void RecordException(Exception e)
        {
            (_exceptions ??= []).Add(e);
            try
            {
                Cancellation.Cancel();
            }
            catch (AggregateException ex)
            {
                foreach (var inEx in ex.InnerExceptions)
                    _exceptions.Add(inEx);
            }
        }

        public void Complete()
        {
            if (_externalCancellationToken.IsCancellationRequested)
            {
                TrySetCanceled(_externalCancellationToken);
            }
            else if (_exceptions is null)
            {
                TrySetResult();
            }
            else
            {
                TrySetException(_exceptions);
            }
        }

        void IThreadPoolWorkItem.Execute()
        {
            if (_executionContext is null)
            {
                _taskBody(this);
            }
            else
            {
                ExecutionContext.Run(_executionContext, static o => ((ForEachAsyncStateWithState<TSource, TState>)o!)._taskBody(o), this);
            }
        }
    }
}

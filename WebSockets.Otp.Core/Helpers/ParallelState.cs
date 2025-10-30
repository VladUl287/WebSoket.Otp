using System.Collections.Concurrent;
using System.Threading.Channels;

namespace WebSockets.Otp.Core.Helpers;

public static class ParallelState
{
    public static Task ForEachStateAsync<TSource, TState>(
        IAsyncEnumerable<TSource> source, int dop, TaskScheduler scheduler,
        Func<TSource, TState, CancellationToken, ValueTask> action, TState state, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return Task.FromCanceled(token);

        try
        {
            var syncState = new SyncForEachAsyncEnumerable<TSource, TState>(
                source, TaskBody<TSource, TState>, dop,
                scheduler, action, state, token);

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
        var syncState = (SyncForEachAsyncEnumerable<TSource, TState>)o;
        var nextWorkerRunning = false;

        try
        {
            while (!syncState.Cancellation.IsCancellationRequested)
            {
                var element = await syncState.TryGetNextAsync();
                if (element is null)
                    break;

                if (!nextWorkerRunning)
                {
                    nextWorkerRunning = true;
                    syncState.TryRunWorker();
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
                await syncState.DisposeAsync();
                syncState.Complete();
            }
        }
    }

    private sealed class SyncForEachAsyncEnumerable<TSource, TState>(
        IAsyncEnumerable<TSource> source,
        Func<object, Task> taskBody,
        int dop,
        TaskScheduler scheduler,
        Func<TSource, TState, CancellationToken, ValueTask> body,
        TState state,
        CancellationToken token) : ForEachAsyncStateWithState<TSource, TState>(taskBody, dop, scheduler, token, body), IAsyncDisposable
    {
        private readonly IAsyncEnumerator<TSource> _enumerator = source.GetAsyncEnumerator(token);
        private readonly SemaphoreSlim _lock = new(initialCount: 1, maxCount: 1);

        public readonly TState State = state;

        public override async ValueTask<TSource?> TryGetNextAsync()
        {
            await _lock.WaitAsync(CancellationToken.None); //TODO: spin wait + semaphore lock + thread.yield?
            try
            {
                if (Cancellation.IsCancellationRequested || !await _enumerator.MoveNextAsync())
                    return default;

                return _enumerator.Current;
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            _lock.Dispose();
            _registration.Dispose();
            return _enumerator.DisposeAsync();
        }
    }

    private abstract class ForEachAsyncStateWithState<TSource, TState> : TaskCompletionSource, IThreadPoolWorkItem
    {
        private readonly CancellationToken _externalCancellationToken;

        protected readonly CancellationTokenRegistration _registration;

        private readonly Func<object, Task> _taskBody;

        private readonly TaskScheduler _scheduler;

        private readonly ExecutionContext? _executionContext;

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

            _remainingDop = dop < 0 ? 1 : dop;
            _scheduler = scheduler;

            if (scheduler == TaskScheduler.Default)
            {
                _executionContext = ExecutionContext.Capture();
            }

            _externalCancellationToken = cancellationToken;
            _registration = cancellationToken.UnsafeRegister(static o => ((ForEachAsyncStateWithState<TSource, TState>)o!).Cancellation.Cancel(), this);
        }

        public void TryRunWorker()
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

        public abstract ValueTask<TSource?> TryGetNextAsync();
    }


    private sealed class SyncForEachChannel<TSource, TState> : ForEachAsyncStateWithState<TSource, TState>, IDisposable
    {
        public readonly Channel<TSource> Channel;
        public readonly TState State;

        public SyncForEachChannel(
            Channel<TSource> source, Func<object, Task> taskBody,
            int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
            Func<TSource, TState, CancellationToken, ValueTask> body, TState state) : base(taskBody, dop, scheduler, cancellationToken, body)
        {
            Channel = source;
            State = state;
        }

        public override ValueTask<TSource?> TryGetNextAsync() => Channel.Reader.ReadAsync(Cancellation.Token);

        public void Dispose()
        {
            _registration.Dispose();
            Channel.Writer.Complete();
        }
    }
}

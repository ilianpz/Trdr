namespace Trdr.Async;

    public sealed class AsyncManualResetEventEx
    {
        private readonly object _mutex = new();

        private readonly bool _asyncCompletion;
        private TaskCompletionSource _tcs;

        public AsyncManualResetEventEx(bool set, bool asyncCompletion)
        {
            _asyncCompletion = asyncCompletion;
            _tcs = CreateTaskCompletionSource();
            if (set)
                _tcs.TrySetResult();
        }

        public AsyncManualResetEventEx(bool asyncCompletion = false)
            : this(false, asyncCompletion)
        {
        }

        public Task Wait()
        {
            lock (_mutex)
            {
                return _tcs.Task;
            }
        }

        public Task Wait(CancellationToken cancellationToken)
        {
            var waitTask = Wait();
            if (waitTask.IsCompleted)
                return waitTask;
            return waitTask.WaitAsync(cancellationToken);
        }

        public void Set()
        {
            lock (_mutex)
            {
                _tcs.TrySetResult();
            }
        }

        public void Reset()
        {
            lock (_mutex)
            {
                if (_tcs.Task.IsCompleted)
                    _tcs = CreateTaskCompletionSource();
            }
        }

        private TaskCompletionSource CreateTaskCompletionSource()
        {
            return new TaskCompletionSource(_asyncCompletion ? TaskCreationOptions.RunContinuationsAsynchronously : 0);
        }
    }
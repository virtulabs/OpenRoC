namespace liboroc
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class ExecutorService : IDisposable
    {
        private readonly List<Task> pending;
        public Action<Exception> ExceptionReceived;
        private readonly object _lock = new object();

        public ExecutorService()
        {
            pending = new List<Task>();
        }

        public void Accept(Action action)
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                pending.Add(Task.Run(() =>
                {
                    try { action(); }
                    catch (Exception ex)
                    { ExceptionReceived?.Invoke(ex); }
                }));
            }
        }

        public void Wait()
        {
            if (IsDisposed)
                return;

            lock (_lock)
            {
                Task.WaitAll(pending.ToArray());
                pending.ForEach(task => task.Dispose());
                pending.Clear();
            }
        }

        #region IDisposable Support

        public bool IsDisposed { get; private set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Wait();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
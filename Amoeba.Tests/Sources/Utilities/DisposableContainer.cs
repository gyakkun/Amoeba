using System;
using Omnius.Base;

namespace Amoeba.Tests
{
    class DisposableContainer<T> : ManagerBase
    {
        private T _value;
        private Action _action;

        public DisposableContainer(T value, Action action)
        {
            _value = value;
            _action = action;
        }

        public T Value
        {
            get
            {
                return _value;
            }
        }

        private volatile bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _action?.Invoke();
            }
        }
    }
}

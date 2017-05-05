using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class NameEditDialogViewModel : ManagerBase
    {
        public ReactiveProperty<string> Name { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public NameEditDialogViewModel()
        {
            this.Init();
        }

        public void Init()
        {
            this.Name = new ReactiveProperty<string>().AddTo(_disposable);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}

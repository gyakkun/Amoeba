using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Service;
using System.Reactive.Linq;

namespace Amoeba.Interface
{
    class ChatThreadViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatThreadInfo Model { get; private set; }

        public ChatThreadViewModel(TreeViewModelBase parent, ChatThreadInfo model)
            : base(parent)
        {
            this.Model = model;

            this.Name = model.ObserveProperty(n => n.Tag).Select(n => MessageConverter.ToString(n)).ToReactiveProperty().AddTo(_disposable);
        }

        public override string DragFormat { get { return "Chat"; } }

        public override bool TryAdd(object value)
        {
            return false;
        }

        public override bool TryRemove(object value)
        {
            return false;
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

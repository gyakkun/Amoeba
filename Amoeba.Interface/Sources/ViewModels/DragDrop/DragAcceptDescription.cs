using System;
using System.Windows;

namespace Amoeba.Interface
{
    // http://b.starwing.net/?p=131
    public sealed class DragAcceptDescription
    {
        public event Action<DragEventArgs> DragOver;

        public void OnDragOver(DragEventArgs dragEventArgs)
        {
            this.DragOver?.Invoke(dragEventArgs);
        }

        public event Action<DragEventArgs> DragDrop;

        public void OnDrop(DragEventArgs dragEventArgs)
        {
            this.DragDrop?.Invoke(dragEventArgs);
        }
    }
}
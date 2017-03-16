using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Reactive.Bindings;

namespace Amoeba.Interface
{
    abstract class TreeViewModelBase : ManagerBase, IDragable, IDropable
    {
        public ReactiveProperty<string> Name { get; protected set; }
        public ReactiveProperty<bool> IsSelected { get; protected set; }
        public ReactiveProperty<bool> IsExpanded { get; protected set; }

        public TreeViewModelBase(TreeViewModelBase parent)
        {
            this.Parent = parent;
        }

        public TreeViewModelBase Parent { get; private set; }

        public IEnumerable<TreeViewModelBase> GetAncestors()
        {
            var list = new LinkedList<TreeViewModelBase>();
            list.AddFirst(this);

            for (;;)
            {
                var parent = list.First.Value.Parent;
                if (parent == null) break;

                list.AddFirst(parent);
            }

            return list;
        }

        public abstract string DragFormat { get; }
        public abstract bool TryAdd(object value);
        public abstract bool TryRemove(object value);
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    sealed class SignatureTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isExpanded;
        private SignatureTreeItem _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public SignatureTreeViewModel(TreeViewModelBase parent, SignatureTreeItem value)
            : base(parent)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _children = new ObservableCollectionEx<TreeViewModelBase>();
            _readonlyChildren = new ReadOnlyObservableCollection<TreeViewModelBase>(_children);

            this.Value = value;
        }

        private void Update()
        {
            this.NotifyPropertyChanged(nameof(this.Name));

            {
                var tempList = new List<SignatureTreeViewModel>();

                foreach (var item in _value.Children)
                {
                    tempList.Add(new SignatureTreeViewModel(this, item));
                }

                tempList.Sort((x, y) =>
                {
                    int c = x.Value.LinkItem.Signature.CompareTo(y.Value.LinkItem.Signature);
                    if (c != 0) return c;

                    return x.GetHashCode().CompareTo(y.GetHashCode());
                });

                _children.Clear();
                _children.AddRange(tempList);
            }
        }

        public override string Name
        {
            get
            {
                return _value.LinkItem.Signature;
            }
        }

        public override bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;

                    this.NotifyPropertyChanged(nameof(this.IsSelected));
                }
            }
        }

        public override bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;

                    this.NotifyPropertyChanged(nameof(this.IsExpanded));
                }
            }
        }

        public override IReadOnlyCollection<TreeViewModelBase> Children
        {
            get
            {
                return _readonlyChildren;
            }
        }

        public SignatureTreeItem Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;

                this.Update();
            }
        }
    }
}

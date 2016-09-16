using System;
using System.Windows;
using System.Windows.Controls;
using Amoeba.Properties;
using Library.Collections;
using Library.Net.Amoeba;
using Library;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Amoeba.Windows
{
    class ChatTreeViewModel : TreeViewModelBase
    {
        private bool _isSelected;
        private bool _isExpanded;
        private bool _isHit;
        private ChatTreeItem _value;

        private ReadOnlyObservableCollection<TreeViewModelBase> _readonlyChildren;
        private ObservableCollectionEx<TreeViewModelBase> _children;

        public ChatTreeViewModel(TreeViewModelBase parent, ChatTreeItem value)
            : base(parent)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            _children = new ObservableCollectionEx<TreeViewModelBase>();
            _readonlyChildren = new ReadOnlyObservableCollection<TreeViewModelBase>(_children);

            this.Value = value;
        }

        public void Update()
        {
            this.NotifyPropertyChanged(nameof(this.Name));
        }

        public override string Name
        {
            get
            {
                var sb = new StringBuilder();

                {
                    sb.Append(this.Value.Tag.Name);
                }

                sb.Append(' ');

                {
                    sb.Append(string.Format("({0})", this.Value.MulticastMessages.Count));
                    if (!_value.IsTrustEnabled) sb.Append('!');
                }

                sb.Append(" - ");

                {
                    sb.Append(NetworkConverter.ToBase64UrlString(this.Value.Tag.Id));
                }

                return sb.ToString();
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

        public bool IsHit
        {
            get
            {
                return _isHit;
            }
            set
            {
                if (value != _isHit)
                {
                    _isHit = value;

                    this.NotifyPropertyChanged(nameof(this.IsHit));
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

        public ChatTreeItem Value
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

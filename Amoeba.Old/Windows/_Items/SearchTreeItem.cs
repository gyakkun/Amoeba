using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Collections;
using System;
using Library.Net.Amoeba;
using System.Text.RegularExpressions;
using Library.Io;

namespace Amoeba.Windows
{
    [DataContract(Name = "SearchTreeItem")]
    class SearchTreeItem : ICloneable<SearchTreeItem>, IThisLock
    {
        private SearchItem _searchItem;
        private LockedList<SearchTreeItem> _children;
        private bool _isExpanded = true;

        private static readonly object _initializeLock = new object();
        private volatile object _thisLock;

        public SearchTreeItem(SearchItem searchItem)
        {
            this.SearchItem = searchItem;
        }

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _searchItem;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _searchItem = value;
                }
            }
        }

        [DataMember(Name = "Items")]
        public LockedList<SearchTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<SearchTreeItem>();

                    return _children;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        #region ICloneable<SearchTreeItem>

        public SearchTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                return JsonUtils.Clone(this);
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }
}

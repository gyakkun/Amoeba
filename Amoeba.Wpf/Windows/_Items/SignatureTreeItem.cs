using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    [DataContract(Name = "SignatureTreeItem")]
    class SignatureTreeItem : ICloneable<SignatureTreeItem>, IThisLock
    {
        private LinkItem _linkItem;
        private LockedList<SignatureTreeItem> _children;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SignatureTreeItem(LinkItem sectionLinkItemInfo)
        {
            this.LinkItem = sectionLinkItemInfo;
        }

        public SignatureTreeItem Search(Func<SignatureTreeItem, bool> predicate)
        {
            var children = this.Children
                .Select(n => n.Search(predicate))
                .Where(n => n != null).ToList();

            if (children.Any())
            {
                var result = new SignatureTreeItem(this.LinkItem);
                result.Children.AddRange(children);
                return result;
            }
            else if (predicate(this))
            {
                var result = new SignatureTreeItem(this.LinkItem);
                return result;
            }

            return null;
        }

        [DataMember(Name = "LinkItem")]
        public LinkItem LinkItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _linkItem;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _linkItem = value;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<SignatureTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<SignatureTreeItem>();

                    return _children;
                }
            }
        }

        #region ICloneable<SignatureTreeItem>

        public SignatureTreeItem Clone()
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

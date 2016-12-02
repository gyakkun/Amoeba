using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;
using Library.Collections;
using Library.Io;

namespace Amoeba.Windows
{
    [DataContract(Name = "ChatTreeItem")]
    class ChatTreeItem : ICloneable<ChatTreeItem>, IThisLock
    {
        private Tag _tag;

        private bool _isTrustEnabled = true;

        private LockedHashDictionary<MulticastMessageItem, MulticastMessageOption> _multicastMessages;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatTreeItem(Tag tag)
        {
            this.Tag = tag;
        }

        [DataMember(Name = "Tag")]
        public Tag Tag
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _tag;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
                }
            }
        }

        [DataMember(Name = "IsTrustEnabled")]
        public bool IsTrustEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isTrustEnabled;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isTrustEnabled = value;
                }
            }
        }

        [DataMember(Name = "MulticastMessages-2")]
        public LockedHashDictionary<MulticastMessageItem, MulticastMessageOption> MulticastMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_multicastMessages == null)
                        _multicastMessages = new LockedHashDictionary<MulticastMessageItem, MulticastMessageOption>();

                    return _multicastMessages;
                }
            }
        }

        #region ICloneable<ChatTreeItem>

        public ChatTreeItem Clone()
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

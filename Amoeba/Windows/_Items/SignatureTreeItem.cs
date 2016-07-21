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
    [DataContract(Name = "SignatureTreeItem", Namespace = "http://Amoeba/Windows")]
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
            if (this.Children == null || this.Children.Count == 0)
            {
                if (predicate(this)) return this;
                else return null;
            }
            else
            {
                var results = this.Children
                    .Select(n => n.Search(predicate))
                    .Where(n => n != null).ToList();

                if (results.Any())
                {
                    var result = new SignatureTreeItem(this.LinkItem);
                    result.Children.AddRange(results);

                    return result;
                }

                return null;
            }
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
                var ds = new DataContractSerializer(typeof(SignatureTreeItem));

                using (BufferStream stream = new BufferStream(BufferManager.Instance))
                {
                    using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                    using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(xmlDictionaryWriter, this);
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SignatureTreeItem)ds.ReadObject(xmlDictionaryReader);
                    }
                }
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

using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Collections;
using System;
using Library.Net.Amoeba;
using System.Text.RegularExpressions;

namespace Amoeba.Windows
{
    [DataContract(Name = "SearchTreeItem", Namespace = "http://Amoeba/Windows")]
    class SearchTreeItem : IDeepCloneable<SearchTreeItem>, IThisLock
    {
        private SearchItem _searchItem;
        private LockedList<SearchTreeItem> _children;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

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
            set
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

        #region IDeepClone<SearchTreeItem>

        public SearchTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchTreeItem)ds.ReadObject(textDictionaryReader);
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
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}

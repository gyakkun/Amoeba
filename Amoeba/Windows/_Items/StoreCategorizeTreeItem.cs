using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Collections;

namespace Amoeba.Windows
{
    [DataContract(Name = "StoreCategorizeTreeItem", Namespace = "http://Amoeba/Windows")]
    class StoreCategorizeTreeItem : IDeepCloneable<StoreCategorizeTreeItem>, IThisLock
    {
        private string _name;
        private LockedList<StoreTreeItem> _storeTreeItems;
        private LockedList<StoreCategorizeTreeItem> _children;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "StoreTreeItems")]
        public LockedList<StoreTreeItem> StoreTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_storeTreeItems == null)
                        _storeTreeItems = new LockedList<StoreTreeItem>();

                    return _storeTreeItems;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<StoreCategorizeTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<StoreCategorizeTreeItem>();

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

        #region IDeepClone<StoreCategorizeTreeItem>

        public StoreCategorizeTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(StoreCategorizeTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (StoreCategorizeTreeItem)ds.ReadObject(textDictionaryReader);
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

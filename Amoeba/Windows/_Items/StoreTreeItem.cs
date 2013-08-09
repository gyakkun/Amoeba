using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    [DataContract(Name = "StoreTreeInfo", Namespace = "http://Amoeba/Windows")]
    class StoreTreeItem : IDeepCloneable<StoreTreeItem>, IThisLock
    {
        private string _signature = null;
        private BoxCollection _boxes = null;
        private bool _isExpanded = true;
        private bool _isUpdated = false;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "UploadSignature")]
        public string Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _signature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _signature = value;
                }
            }
        }

        [DataMember(Name = "Boxes")]
        public BoxCollection Boxes
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_boxes == null)
                        _boxes = new BoxCollection();

                    return _boxes;
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

        [DataMember(Name = "IsUpdated")]
        public bool IsUpdated
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isUpdated;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isUpdated = value;
                }
            }
        }

        #region IDeepClone<StoreTreeItem>

        public StoreTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(StoreTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (StoreTreeItem)ds.ReadObject(textDictionaryReader);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utilities;

namespace Amoeba.Core
{
    class MetadataManager
    {
        // Type, CreatorSignature
        private Dictionary<string, Dictionary<Signature, BroadcastMetadata>> _broadcastMetadatas = new Dictionary<string, Dictionary<Signature, BroadcastMetadata>>();
        // Type, Signature, CreatorSignature
        private Dictionary<string, Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMetadata>>>> _unicastMetadatas = new Dictionary<string, Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMetadata>>>>();
        // Type, Tag, CreatorSignature
        private Dictionary<string, Dictionary<Tag, Dictionary<Signature, HashSet<MulticastMetadata>>>> _multicastMetadatas = new Dictionary<string, Dictionary<Tag, Dictionary<Signature, HashSet<MulticastMetadata>>>>();

        // UpdateTime
        private Dictionary<string, DateTime> _broadcastTypes = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> _unicastTypes = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> _multicastTypes = new Dictionary<string, DateTime>();

        private readonly object _thisLock = new object();

        public MetadataManager()
        {

        }

        public GetSignaturesEventHandler GetLockSignaturesEvent { get; set; }

        private IEnumerable<Signature> OnGetLockSignaturesEvent()
        {
            return this.GetLockSignaturesEvent?.Invoke(this) ?? new Signature[0];
        }

        public GetTagsEventHandler GetLockTagsEvent { get; set; }

        private IEnumerable<Tag> OnGetLockTagsEvent()
        {
            return this.GetLockTagsEvent?.Invoke(this) ?? new Tag[0];
        }

        public void Refresh()
        {
            lock (_thisLock)
            {
                var lockSignatures = new HashSet<Signature>(this.OnGetLockSignaturesEvent());
                var lockTags = new HashSet<Tag>(this.OnGetLockTagsEvent());

                // Broadcast
                {
                    {
                        var hashset = new HashSet<string>(_broadcastTypes.OrderByDescending(n => n.Value).Select(n => n.Key).Take(32));

                        foreach (var key in _broadcastMetadatas.Keys.ToArray())
                        {
                            if (!hashset.Contains(key))
                            {
                                _broadcastMetadatas.Remove(key);
                            }
                        }
                    }

                    foreach (var dic in _broadcastMetadatas.Values)
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 1024))
                        {
                            dic.Remove(key);
                        }
                    }
                }

                // Unicast
                {
                    {
                        var hashset = new HashSet<string>(_unicastTypes.OrderByDescending(n => n.Value).Select(n => n.Key).Take(32));

                        foreach (var key in _unicastMetadatas.Keys.ToArray())
                        {
                            if (!hashset.Contains(key))
                            {
                                _unicastMetadatas.Remove(key);
                            }
                        }
                    }

                    foreach (var dic in _unicastMetadatas.Values)
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 1024))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var dic in _unicastMetadatas.Values.SelectMany(n => n.Values))
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 32))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var hashset in _unicastMetadatas.Values.SelectMany(n => n.Values.SelectMany(m => m.Values)).ToArray())
                    {
                        if (hashset.Count <= 32) continue;

                        var list = hashset.ToList();
                        list.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));

                        foreach (var value in list.Take(list.Count - 32))
                        {
                            hashset.Remove(value);
                        }
                    }
                }

                // Multicast
                {
                    {
                        var hashset = new HashSet<string>(_multicastTypes.OrderByDescending(n => n.Value).Select(n => n.Key).Take(32));

                        foreach (var key in _multicastMetadatas.Keys.ToArray())
                        {
                            if (!hashset.Contains(key))
                            {
                                _multicastMetadatas.Remove(key);
                            }
                        }
                    }

                    foreach (var dic in _multicastMetadatas.Values)
                    {
                        var keys = dic.Keys.Where(n => !lockTags.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 1024))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var dic in _multicastMetadatas.Values.SelectMany(n => n.Values))
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 32))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var hashset in _multicastMetadatas.Values.SelectMany(n => n.Values.SelectMany(m => m.Values)).ToArray())
                    {
                        if (hashset.Count <= 32) continue;

                        var list = hashset.ToList();
                        list.Sort((x, y) => x.CreationTime.CompareTo(y.CreationTime));

                        foreach (var value in list.Take(list.Count - 32))
                        {
                            hashset.Remove(value);
                        }
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_thisLock)
                {
                    int count = 0;

                    count += _broadcastMetadatas.Values.Sum(n => n.Count);
                    count += _unicastMetadatas.Values.Sum(n => n.Values.Sum(m => m.Values.Sum(o => o.Count)));
                    count += _multicastMetadatas.Values.Sum(n => n.Values.Sum(m => m.Values.Sum(o => o.Count)));

                    return count;
                }
            }
        }

        public IEnumerable<Signature> GetBroadcastSignatures()
        {
            lock (_thisLock)
            {
                var hashset = new HashSet<Signature>();

                hashset.UnionWith(_broadcastMetadatas.Values.SelectMany(n => n.Keys));

                return hashset;
            }
        }

        public IEnumerable<Signature> GetUnicastSignatures()
        {
            lock (_thisLock)
            {
                var hashset = new HashSet<Signature>();

                hashset.UnionWith(_unicastMetadatas.Values.SelectMany(n => n.Keys));

                return hashset;
            }
        }

        public IEnumerable<Tag> GetMulticastTags()
        {
            lock (_thisLock)
            {
                var hashset = new HashSet<Tag>();

                hashset.UnionWith(_multicastMetadatas.Values.SelectMany(n => n.Keys));

                return hashset;
            }
        }

        public IEnumerable<BroadcastMetadata> GetBroadcastMetadatas()
        {
            lock (_thisLock)
            {
                return _broadcastMetadatas.Values.SelectMany(n => n.Values).ToArray();
            }
        }

        public IEnumerable<BroadcastMetadata> GetBroadcastMetadatas(Signature signature)
        {
            lock (_thisLock)
            {
                var list = new List<BroadcastMetadata>();

                foreach (var dic in _broadcastMetadatas.Values)
                {
                    BroadcastMetadata broadcastMetadata;

                    if (dic.TryGetValue(signature, out broadcastMetadata))
                    {
                        list.Add(broadcastMetadata);
                    }
                }

                return list;
            }
        }

        public BroadcastMetadata GetBroadcastMetadata(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _broadcastTypes[type] = DateTime.UtcNow;

                Dictionary<Signature, BroadcastMetadata> dic;

                if (_broadcastMetadatas.TryGetValue(type, out dic))
                {
                    BroadcastMetadata broadcastMetadata;

                    if (dic.TryGetValue(signature, out broadcastMetadata))
                    {
                        return broadcastMetadata;
                    }
                }

                return null;
            }
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas()
        {
            lock (_thisLock)
            {
                return _unicastMetadatas.Values.SelectMany(n => n.Values.SelectMany(m => m.Values.Extract())).ToArray();
            }
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas(Signature signature)
        {
            lock (_thisLock)
            {
                var list = new List<UnicastMetadata>();

                foreach (var dic in _unicastMetadatas.Values)
                {
                    Dictionary<Signature, HashSet<UnicastMetadata>> dic2;

                    if (dic.TryGetValue(signature, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _unicastTypes[type] = DateTime.UtcNow;

                var list = new List<UnicastMetadata>();

                Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMetadata>>> dic;

                if (_unicastMetadatas.TryGetValue(type, out dic))
                {
                    Dictionary<Signature, HashSet<UnicastMetadata>> dic2;

                    if (dic.TryGetValue(signature, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas()
        {
            lock (_thisLock)
            {
                return _multicastMetadatas.Values.SelectMany(n => n.Values.SelectMany(m => m.Values.Extract())).ToArray();
            }
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas(Tag tag)
        {
            lock (_thisLock)
            {
                var list = new List<MulticastMetadata>();

                foreach (var dic in _multicastMetadatas.Values)
                {
                    Dictionary<Signature, HashSet<MulticastMetadata>> dic2;

                    if (dic.TryGetValue(tag, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas(Tag tag, string type)
        {
            lock (_thisLock)
            {
                _multicastTypes[type] = DateTime.UtcNow;

                var list = new List<MulticastMetadata>();

                Dictionary<Tag, Dictionary<Signature, HashSet<MulticastMetadata>>> dic;

                if (_multicastMetadatas.TryGetValue(type, out dic))
                {
                    Dictionary<Signature, HashSet<MulticastMetadata>> dic2;

                    if (dic.TryGetValue(tag, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public bool SetMetadata(BroadcastMetadata broadcastMetadata)
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

                if (broadcastMetadata == null
                    || broadcastMetadata.Type == null
                    || (broadcastMetadata.CreationTime - now).TotalMinutes > 30
                    || broadcastMetadata.Certificate == null) return false;

                Dictionary<Signature, BroadcastMetadata> dic;

                if (!_broadcastMetadatas.TryGetValue(broadcastMetadata.Type, out dic))
                {
                    dic = new Dictionary<Signature, BroadcastMetadata>();
                    _broadcastMetadatas[broadcastMetadata.Type] = dic;
                }

                var signature = broadcastMetadata.Certificate.GetSignature();

                BroadcastMetadata tempMetadata;

                if (!dic.TryGetValue(signature, out tempMetadata)
                    || broadcastMetadata.CreationTime > tempMetadata.CreationTime)
                {
                    if (!broadcastMetadata.VerifyCertificate()) throw new MessageManagerException("Certificate");

                    dic[signature] = broadcastMetadata;
                }

                return true;
            }
        }

        public bool SetMetadata(UnicastMetadata unicastMetadata)
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

                if (unicastMetadata == null
                    || unicastMetadata.Type == null
                    || unicastMetadata.Signature == null
                        || unicastMetadata.Signature.Id == null || unicastMetadata.Signature.Id.Length == 0
                        || string.IsNullOrWhiteSpace(unicastMetadata.Signature.Name)
                    || (unicastMetadata.CreationTime - now).TotalMinutes > 30
                    || unicastMetadata.Certificate == null) return false;

                Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMetadata>>> dic;

                if (!_unicastMetadatas.TryGetValue(unicastMetadata.Type, out dic))
                {
                    dic = new Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMetadata>>>();
                    _unicastMetadatas[unicastMetadata.Type] = dic;
                }

                Dictionary<Signature, HashSet<UnicastMetadata>> dic2;

                if (!dic.TryGetValue(unicastMetadata.Signature, out dic2))
                {
                    dic2 = new Dictionary<Signature, HashSet<UnicastMetadata>>();
                    dic[unicastMetadata.Signature] = dic2;
                }

                var signature = unicastMetadata.Certificate.GetSignature();

                HashSet<UnicastMetadata> hashset;

                if (!dic2.TryGetValue(signature, out hashset))
                {
                    hashset = new HashSet<UnicastMetadata>();
                    dic2[signature] = hashset;
                }

                if (!hashset.Contains(unicastMetadata))
                {
                    if (!unicastMetadata.VerifyCertificate()) throw new MessageManagerException("Certificate");

                    hashset.Add(unicastMetadata);
                }

                return true;
            }
        }

        public bool SetMetadata(MulticastMetadata multicastMetadata)
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

                if (multicastMetadata == null
                    || multicastMetadata.Type == null
                    || multicastMetadata.Tag == null
                        || multicastMetadata.Tag.Id == null || multicastMetadata.Tag.Id.Length == 0
                        || string.IsNullOrWhiteSpace(multicastMetadata.Tag.Name)
                    || (multicastMetadata.CreationTime - now).TotalMinutes > 30
                    || multicastMetadata.Certificate == null) return false;

                Dictionary<Tag, Dictionary<Signature, HashSet<MulticastMetadata>>> dic;

                if (!_multicastMetadatas.TryGetValue(multicastMetadata.Type, out dic))
                {
                    dic = new Dictionary<Tag, Dictionary<Signature, HashSet<MulticastMetadata>>>();
                    _multicastMetadatas[multicastMetadata.Type] = dic;
                }

                Dictionary<Signature, HashSet<MulticastMetadata>> dic2;

                if (!dic.TryGetValue(multicastMetadata.Tag, out dic2))
                {
                    dic2 = new Dictionary<Signature, HashSet<MulticastMetadata>>();
                    dic[multicastMetadata.Tag] = dic2;
                }

                var signature = multicastMetadata.Certificate.GetSignature();

                HashSet<MulticastMetadata> hashset;

                if (!dic2.TryGetValue(signature, out hashset))
                {
                    hashset = new HashSet<MulticastMetadata>();
                    dic2[signature] = hashset;
                }

                if (!hashset.Contains(multicastMetadata))
                {
                    if (!multicastMetadata.VerifyCertificate()) throw new MessageManagerException("Certificate");

                    hashset.Add(multicastMetadata);
                }

                return true;
            }
        }
    }

    [Serializable]
    class MessageManagerException : ManagerException
    {
        public MessageManagerException() : base() { }
        public MessageManagerException(string message) : base(message) { }
        public MessageManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}

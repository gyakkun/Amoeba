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

namespace Amoeba.Core.Network
{
    class MessageManager
    {
        // Type, CreatorSignature
        private Dictionary<string, Dictionary<Signature, BroadcastMessage>> _broadcastMessages = new Dictionary<string, Dictionary<Signature, BroadcastMessage>>();
        // Type, Signature, CreatorSignature
        private Dictionary<string, Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMessage>>>> _unicastMessages = new Dictionary<string, Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMessage>>>>();

        // UpdateTime
        private Dictionary<string, DateTime> _broadcastTypes = new Dictionary<string, DateTime>();
        private Dictionary<string, DateTime> _unicastTypes = new Dictionary<string, DateTime>();

        private readonly object _thisLock = new object();

        public MessageManager()
        {

        }

        public GetSignaturesEventHandler GetLockSignaturesEvent { get; set; }

        private IEnumerable<Signature> OnGetLockSignaturesEvent()
        {
            return this.GetLockSignaturesEvent?.Invoke(this) ?? new Signature[0];
        }

        public void Refresh()
        {
            lock (_thisLock)
            {
                var lockSignatures = new HashSet<Signature>(this.OnGetLockSignaturesEvent());

                // Broadcast
                {
                    {
                        var hashset = new HashSet<string>(_broadcastTypes.OrderByDescending(n => n.Value).Select(n => n.Key).Take(32));

                        foreach (var key in _broadcastMessages.Keys.ToArray())
                        {
                            if (!hashset.Contains(key))
                            {
                                _broadcastMessages.Remove(key);
                            }
                        }
                    }

                    foreach (var dic in _broadcastMessages.Values)
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

                        foreach (var key in _unicastMessages.Keys.ToArray())
                        {
                            if (!hashset.Contains(key))
                            {
                                _unicastMessages.Remove(key);
                            }
                        }
                    }

                    foreach (var dic in _unicastMessages.Values)
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 1024))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var dic in _unicastMessages.Values.SelectMany(n => n.Values))
                    {
                        var keys = dic.Keys.Where(n => !lockSignatures.Contains(n)).ToList();

                        foreach (var key in keys.Randomize().Take(keys.Count - 32))
                        {
                            dic.Remove(key);
                        }
                    }

                    foreach (var hashset in _unicastMessages.Values.SelectMany(n => n.Values.SelectMany(m => m.Values)).ToArray())
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

                    count += _broadcastMessages.Values.Sum(n => n.Count);
                    count += _unicastMessages.Values.Sum(n => n.Values.Sum(m => m.Values.Sum(o => o.Count)));

                    return count;
                }
            }
        }

        public IEnumerable<Signature> GetBroadcastSignatures()
        {
            lock (_thisLock)
            {
                var hashset = new HashSet<Signature>();

                hashset.UnionWith(_broadcastMessages.Values.SelectMany(n => n.Keys));

                return hashset;
            }
        }

        public IEnumerable<Signature> GetUnicastSignatures()
        {
            lock (_thisLock)
            {
                var hashset = new HashSet<Signature>();

                hashset.UnionWith(_unicastMessages.Values.SelectMany(n => n.Keys));

                return hashset;
            }
        }

        public IEnumerable<BroadcastMessage> GetBroadcastMessages()
        {
            lock (_thisLock)
            {
                return _broadcastMessages.Values.SelectMany(n => n.Values).ToArray();
            }
        }

        public IEnumerable<BroadcastMessage> GetBroadcastMessages(Signature signature)
        {
            lock (_thisLock)
            {
                var list = new List<BroadcastMessage>();

                foreach (var dic in _broadcastMessages.Values)
                {
                    BroadcastMessage broadcastMessage;

                    if (dic.TryGetValue(signature, out broadcastMessage))
                    {
                        list.Add(broadcastMessage);
                    }
                }

                return list;
            }
        }

        public BroadcastMessage GetBroadcastMessage(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _broadcastTypes[type] = DateTime.UtcNow;

                Dictionary<Signature, BroadcastMessage> dic;

                if (_broadcastMessages.TryGetValue(type, out dic))
                {
                    BroadcastMessage broadcastMessage;

                    if (dic.TryGetValue(signature, out broadcastMessage))
                    {
                        return broadcastMessage;
                    }
                }

                return null;
            }
        }

        public IEnumerable<UnicastMessage> GetUnicastMessages()
        {
            lock (_thisLock)
            {
                return _unicastMessages.Values.SelectMany(n => n.Values.SelectMany(m => m.Values.Extract())).ToArray();
            }
        }

        public IEnumerable<UnicastMessage> GetUnicastMessages(Signature signature)
        {
            lock (_thisLock)
            {
                var list = new List<UnicastMessage>();

                foreach (var dic in _unicastMessages.Values)
                {
                    Dictionary<Signature, HashSet<UnicastMessage>> dic2;

                    if (dic.TryGetValue(signature, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public IEnumerable<UnicastMessage> GetUnicastMessages(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _unicastTypes[type] = DateTime.UtcNow;

                var list = new List<UnicastMessage>();

                Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMessage>>> dic;

                if (_unicastMessages.TryGetValue(type, out dic))
                {
                    Dictionary<Signature, HashSet<UnicastMessage>> dic2;

                    if (dic.TryGetValue(signature, out dic2))
                    {
                        list.AddRange(dic2.Values.Extract());
                    }
                }

                return list;
            }
        }

        public bool SetMetadata(BroadcastMessage broadcastMessage)
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

                if (broadcastMessage == null
                    || broadcastMessage.Type == null
                    || (broadcastMessage.CreationTime - now).TotalMinutes > 30
                    || broadcastMessage.Certificate == null) return false;

                Dictionary<Signature, BroadcastMessage> dic;

                if (!_broadcastMessages.TryGetValue(broadcastMessage.Type, out dic))
                {
                    dic = new Dictionary<Signature, BroadcastMessage>();
                    _broadcastMessages[broadcastMessage.Type] = dic;
                }

                var signature = broadcastMessage.Certificate.GetSignature();

                BroadcastMessage tempMetadata;

                if (!dic.TryGetValue(signature, out tempMetadata)
                    || broadcastMessage.CreationTime > tempMetadata.CreationTime)
                {
                    if (!broadcastMessage.VerifyCertificate()) throw new MetadataManagerException("Certificate");

                    dic[signature] = broadcastMessage;
                }

                return true;
            }
        }

        public bool SetMetadata(UnicastMessage unicastMessage)
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

                if (unicastMessage == null
                    || unicastMessage.Type == null
                    || unicastMessage.Signature == null
                        || unicastMessage.Signature.Id == null || unicastMessage.Signature.Id.Length == 0
                        || string.IsNullOrWhiteSpace(unicastMessage.Signature.Name)
                    || (unicastMessage.CreationTime - now).TotalMinutes > 30
                    || unicastMessage.Certificate == null) return false;

                Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMessage>>> dic;

                if (!_unicastMessages.TryGetValue(unicastMessage.Type, out dic))
                {
                    dic = new Dictionary<Signature, Dictionary<Signature, HashSet<UnicastMessage>>>();
                    _unicastMessages[unicastMessage.Type] = dic;
                }

                Dictionary<Signature, HashSet<UnicastMessage>> dic2;

                if (!dic.TryGetValue(unicastMessage.Signature, out dic2))
                {
                    dic2 = new Dictionary<Signature, HashSet<UnicastMessage>>();
                    dic[unicastMessage.Signature] = dic2;
                }

                var signature = unicastMessage.Certificate.GetSignature();

                HashSet<UnicastMessage> hashset;

                if (!dic2.TryGetValue(signature, out hashset))
                {
                    hashset = new HashSet<UnicastMessage>();
                    dic2[signature] = hashset;
                }

                if (!hashset.Contains(unicastMessage))
                {
                    if (!unicastMessage.VerifyCertificate()) throw new MetadataManagerException("Certificate");

                    hashset.Add(unicastMessage);
                }

                return true;
            }
        }
    }

    [Serializable]
    class MetadataManagerException : ManagerException
    {
        public MetadataManagerException() : base() { }
        public MetadataManagerException(string message) : base(message) { }
        public MetadataManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}

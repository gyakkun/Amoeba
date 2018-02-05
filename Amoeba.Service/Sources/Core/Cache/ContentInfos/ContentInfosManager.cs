using Amoeba.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;
using Omnius.Utilities;
using Omnius.Collections;
using Omnius.Base;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace Amoeba.Service
{
    partial class CacheManager
    {
        sealed class ContentInfosManager : ISetOperators<Hash>, IEnumerable<ContentInfo>
        {
            private Dictionary<Metadata, ContentInfo> _messageContentInfos;
            private Dictionary<string, ContentInfo> _fileContentInfos;

            private HashMap _hashMap;

            public ContentInfosManager()
            {
                _messageContentInfos = new Dictionary<Metadata, ContentInfo>();
                _fileContentInfos = new Dictionary<string, ContentInfo>();

                _hashMap = new HashMap();
            }

            public void Add(ContentInfo info)
            {
                if (info.ShareInfo == null)
                {
                    _messageContentInfos.Add(info.Metadata, info);
                }
                else
                {
                    _fileContentInfos.Add(info.ShareInfo.Path, info);

                    _hashMap.Add(info.ShareInfo);
                }
            }

            #region Message

            public void RemoveMessageContentInfo(Metadata metadata)
            {
                _messageContentInfos.Remove(metadata);
            }

            public bool ContainsMessageContentInfo(Metadata metadata)
            {
                return _messageContentInfos.ContainsKey(metadata);
            }

            public IEnumerable<ContentInfo> GetMessageContentInfos()
            {
                return _messageContentInfos.Values.ToArray();
            }

            public ContentInfo GetMessageContentInfo(Metadata metadata)
            {
                ContentInfo contentInfo;
                if (!_messageContentInfos.TryGetValue(metadata, out contentInfo)) return null;

                return contentInfo;
            }

            #endregion

            #region Content

            public void RemoveFileContentInfo(string path)
            {
                if (_fileContentInfos.TryGetValue(path, out var ContentInfo))
                {
                    _fileContentInfos.Remove(path);

                    _hashMap.Remove(ContentInfo.ShareInfo);
                }
            }

            public bool ContainsFileContentInfo(string path)
            {
                return _fileContentInfos.ContainsKey(path);
            }

            public IEnumerable<ContentInfo> GetFileContentInfos()
            {
                return _fileContentInfos.Values.ToArray();
            }

            public ContentInfo GetFileContentInfo(string path)
            {
                ContentInfo contentInfo;
                if (!_fileContentInfos.TryGetValue(path, out contentInfo)) return null;

                return contentInfo;
            }

            #endregion

            #region Hash

            public bool Contains(Hash hash)
            {
                return _hashMap.Contains(hash);
            }

            public IEnumerable<Hash> IntersectFrom(IEnumerable<Hash> collection)
            {
                foreach (var hash in collection)
                {
                    if (_hashMap.Contains(hash))
                    {
                        yield return hash;
                    }
                }
            }

            public IEnumerable<Hash> ExceptFrom(IEnumerable<Hash> collection)
            {
                foreach (var hash in collection)
                {
                    if (!_hashMap.Contains(hash))
                    {
                        yield return hash;
                    }
                }
            }

            public ShareInfo GetShareInfo(Hash hash)
            {
                return _hashMap.Get(hash);
            }

            public IEnumerable<Hash> GetHashes()
            {
                return _hashMap.ToArray();
            }

            #endregion

            #region IEnumerable<ContentInfo>

            public IEnumerator<ContentInfo> GetEnumerator()
            {
                foreach (var info in CollectionUtils.Unite(_messageContentInfos.Values, _fileContentInfos.Values))
                {
                    yield return info;
                }
            }

            #endregion

            #region IEnumerable

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            sealed class HashMap
            {
                private Dictionary<Hash, SmallList<ShareInfo>> _map;

                public HashMap()
                {
                    _map = new Dictionary<Hash, SmallList<ShareInfo>>();
                }

                public void Add(ShareInfo info)
                {
                    foreach (var hash in info.Hashes)
                    {
                        _map.GetOrAdd(hash, (_) => new SmallList<ShareInfo>()).Add(info);
                    }
                }

                public void Remove(ShareInfo info)
                {
                    foreach (var hash in info.Hashes)
                    {
                        if (_map.TryGetValue(hash, out var infos))
                        {
                            infos.Remove(info);

                            if (infos.Count == 0)
                            {
                                _map.Remove(hash);
                            }
                        }
                    }
                }

                public ShareInfo Get(Hash hash)
                {
                    if (_map.TryGetValue(hash, out var infos))
                    {
                        return infos[0];
                    }

                    return null;
                }

                public bool Contains(Hash hash)
                {
                    return _map.ContainsKey(hash);
                }

                public Hash[] ToArray()
                {
                    return _map.Keys.ToArray();
                }
            }
        }

        [DataContract(Name = nameof(ContentInfo))]
        sealed class ContentInfo
        {
            private DateTime _creationTime;
            private TimeSpan _lifeSpan;

            private Metadata _metadata;
            private HashCollection _lockedHashes;
            private ShareInfo _shareInfo;

            public ContentInfo(DateTime creationTime, TimeSpan lifeTime, Metadata metadata, IEnumerable<Hash> lockedHashes, ShareInfo shareInfo)
            {
                this.CreationTime = creationTime;
                this.LifeSpan = lifeTime;

                this.Metadata = metadata;
                if (lockedHashes != null) this.ProtectedLockedHashes.AddRange(lockedHashes);
                this.ShareInfo = shareInfo;
            }

            [DataMember(Name = nameof(CreationTime))]
            public DateTime CreationTime
            {
                get
                {
                    return _creationTime;
                }
                private set
                {
                    _creationTime = value;
                }
            }

            [DataMember(Name = nameof(LifeSpan))]
            public TimeSpan LifeSpan
            {
                get
                {
                    return _lifeSpan;
                }
                private set
                {
                    _lifeSpan = value;
                }
            }

            [DataMember(Name = nameof(Metadata))]
            public Metadata Metadata
            {
                get
                {
                    return _metadata;
                }
                private set
                {
                    _metadata = value;
                }
            }

            private volatile ReadOnlyCollection<Hash> _readOnlyLockedHashes;

            public IEnumerable<Hash> LockedHashes
            {
                get
                {
                    if (_readOnlyLockedHashes == null)
                        _readOnlyLockedHashes = new ReadOnlyCollection<Hash>(this.ProtectedLockedHashes);

                    return _readOnlyLockedHashes;
                }
            }

            [DataMember(Name = nameof(LockedHashes))]
            private HashCollection ProtectedLockedHashes
            {
                get
                {
                    if (_lockedHashes == null)
                        _lockedHashes = new HashCollection();

                    return _lockedHashes;
                }
            }

            [DataMember(Name = nameof(ShareInfo))]
            public ShareInfo ShareInfo
            {
                get
                {
                    return _shareInfo;
                }
                private set
                {
                    _shareInfo = value;
                }
            }
        }

        [DataContract(Name = nameof(ShareInfo))]
        sealed class ShareInfo
        {
            private string _path;
            private long _fileLength;
            private int _blockLength;
            private HashCollection _hashes;

            public ShareInfo(string path, long fileLength, int blockLength, IEnumerable<Hash> hashes)
            {
                this.Path = path;
                this.FileLength = fileLength;
                this.BlockLength = blockLength;
                if (hashes != null) this.ProtectedHashes.AddRange(hashes);
            }

            [DataMember(Name = nameof(Path))]
            public string Path
            {
                get
                {
                    return _path;
                }
                private set
                {
                    _path = value;
                }
            }

            [DataMember(Name = nameof(FileLength))]
            public long FileLength
            {
                get
                {
                    return _fileLength;
                }
                private set
                {
                    _fileLength = value;
                }
            }

            [DataMember(Name = nameof(BlockLength))]
            public int BlockLength
            {
                get
                {
                    return _blockLength;
                }
                private set
                {
                    _blockLength = value;
                }
            }

            private volatile ReadOnlyCollection<Hash> _readOnlyHashes;

            public IEnumerable<Hash> Hashes
            {
                get
                {
                    if (_readOnlyHashes == null)
                        _readOnlyHashes = new ReadOnlyCollection<Hash>(this.ProtectedHashes);

                    return _readOnlyHashes;
                }
            }

            [DataMember(Name = nameof(Hashes))]
            private HashCollection ProtectedHashes
            {
                get
                {
                    if (_hashes == null)
                        _hashes = new HashCollection();

                    return _hashes;
                }
            }

            #region Hash to Index

            private Dictionary<Hash, int> _hashMap = null;

            public int GetIndex(Hash hash)
            {
                if (_hashMap == null)
                {
                    _hashMap = new Dictionary<Hash, int>();

                    for (int i = 0; i < this.ProtectedHashes.Count; i++)
                    {
                        _hashMap[this.ProtectedHashes[i]] = i;
                    }
                }

                {
                    int result;
                    if (!_hashMap.TryGetValue(hash, out result)) return -1;

                    return result;
                }
            }

            #endregion
        }
    }
}

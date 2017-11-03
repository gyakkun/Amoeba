using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Correction;
using Omnius.Io;
using Omnius.Messaging;
using Omnius.Security;

namespace Amoeba.Service
{
    interface ISetOperators<T>
    {
        IEnumerable<T> IntersectFrom(IEnumerable<T> collection);
        IEnumerable<T> ExceptFrom(IEnumerable<T> collection);
    }

    class CacheManager : ManagerBase, ISettings, ISetOperators<Hash>, IEnumerable<Hash>
    {
        private Stream _fileStream;

        private BitmapManager _bitmapManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private long _size;
        private Dictionary<Hash, ClusterInfo> _clusterIndex;

        private bool _spaceSectors_Initialized;
        private HashSet<long> _spaceSectors = new HashSet<long>();

        private Dictionary<Hash, int> _lockedHashes = new Dictionary<Hash, int>();

        private CacheInfoManager _cacheInfoManager;

        private EventQueue<Hash> _addedBlockEventQueue = new EventQueue<Hash>(new TimeSpan(0, 0, 3));
        private EventQueue<Hash> _removedBlockEventQueue = new EventQueue<Hash>(new TimeSpan(0, 0, 3));
        private EventQueue<Hash> _importedBlockEventQueue = new EventQueue<Hash>(new TimeSpan(0, 0, 3));

        private WatchTimer _watchTimer;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public static readonly int SectorSize = 1024 * 256;
        public static readonly int SpaceSectorCount = 4 * 1024; // SectorSize * 4 * 256 = 256MB

        private readonly int _threadCount = 4;

        public CacheManager(string configPath, string blocksPath, BufferManager bufferManager)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const FileOptions FileFlagNoBuffering = (FileOptions)0x20000000;
                _fileStream = new BufferedStream(new FileStream(blocksPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, CacheManager.SectorSize, FileFlagNoBuffering));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _fileStream = new BufferedStream(new FileStream(blocksPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, CacheManager.SectorSize));
            }

            _bitmapManager = new BitmapManager(bufferManager);
            _bufferManager = bufferManager;

            _settings = new Settings(configPath);

            _cacheInfoManager = new CacheInfoManager();

            _watchTimer = new WatchTimer(this.WatchTimer);
        }

        private static long Roundup(long value, long unit)
        {
            if (value % unit == 0) return value;
            else return ((value / unit) + 1) * unit;
        }

        private void WatchTimer()
        {
            this.CheckInformation();
            this.CheckMessages();
            this.CheckContents();
        }

        private volatile Info _info = new Info();

        public class Info
        {
            public long BlockCount { get; set; }
            public long UsingSpace { get; set; }
            public long LockSpace { get; set; }
            public long FreeSpace { get; set; }
        }

        private void CheckInformation()
        {
            lock (_lockObject)
            {
                _info.BlockCount = this.Count;
                _info.UsingSpace = _fileStream.Length;

                {
                    var usingHashes = new HashSet<Hash>();
                    usingHashes.UnionWith(_lockedHashes.Keys);
                    usingHashes.UnionWith(_cacheInfoManager.Select(n => n.LockedHashes).Extract());

                    long size = 0;

                    foreach (var hash in usingHashes)
                    {
                        if (_clusterIndex.TryGetValue(hash, out var clusterInfo))
                        {
                            size += clusterInfo.Indexes.Length * CacheManager.SectorSize;
                        }
                    }

                    _info.LockSpace = size;
                }

                _info.FreeSpace = this.Size - _info.LockSpace;
            }
        }

        public CacheReport Report
        {
            get
            {
                lock (_lockObject)
                {
                    return new CacheReport(_info.BlockCount, _info.UsingSpace, _info.LockSpace, _info.FreeSpace);
                }
            }
        }

        public IEnumerable<CacheContentReport> GetCacheContentReports()
        {
            lock (_lockObject)
            {
                var list = new List<CacheContentReport>();

                foreach (var info in _cacheInfoManager.GetContentCacheInfos())
                {
                    list.Add(new CacheContentReport(info.CreationTime, info.ShareInfo.FileLength, info.Metadata, info.ShareInfo.Path));
                }

                return list;
            }
        }

        public long Size
        {
            get
            {
                lock (_lockObject)
                {
                    return _size;
                }
            }
        }

        public long Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _clusterIndex.Count;
                }
            }
        }

        public event Action<IEnumerable<Hash>> AddedBlockEvents
        {
            add
            {
                _addedBlockEventQueue.Events += value;
            }
            remove
            {
                _addedBlockEventQueue.Events -= value;
            }
        }

        public event Action<IEnumerable<Hash>> RemovedBlockEvents
        {
            add
            {
                _removedBlockEventQueue.Events += value;
            }
            remove
            {
                _removedBlockEventQueue.Events -= value;
            }
        }

        public event Action<IEnumerable<Hash>> ImportedBlockEvents
        {
            add
            {
                _importedBlockEventQueue.Events += value;
            }
            remove
            {
                _importedBlockEventQueue.Events -= value;
            }
        }

        private void CheckSpace(int sectorCount)
        {
            lock (_lockObject)
            {
                if (!_spaceSectors_Initialized)
                {
                    _bitmapManager.SetLength(this.Size / CacheManager.SectorSize);

                    foreach (var clusterInfo in _clusterIndex.Values)
                    {
                        foreach (long sector in clusterInfo.Indexes)
                        {
                            _bitmapManager.Set(sector, true);
                        }
                    }

                    _spaceSectors_Initialized = true;
                }

                if (_spaceSectors.Count < sectorCount)
                {
                    for (long i = 0; i < _bitmapManager.Length; i++)
                    {
                        if (!_bitmapManager.Get(i))
                        {
                            _spaceSectors.Add(i);
                            if (_spaceSectors.Count >= sectorCount) break;
                        }
                    }
                }
            }
        }

        private void CreatingSpace()
        {
            lock (_lockObject)
            {
                this.CheckSpace(CacheManager.SpaceSectorCount);
                if (CacheManager.SpaceSectorCount <= _spaceSectors.Count) return;

                var usingHashes = new HashSet<Hash>();
                usingHashes.UnionWith(_lockedHashes.Keys);
                usingHashes.UnionWith(_cacheInfoManager.Select(n => n.LockedHashes).Extract());

                var removePairs = _clusterIndex
                    .Where(n => !usingHashes.Contains(n.Key))
                    .ToList();

                removePairs.Sort((x, y) =>
                {
                    return x.Value.UpdateTime.CompareTo(y.Value.UpdateTime);
                });

                foreach (var hash in removePairs.Select(n => n.Key))
                {
                    if (CacheManager.SpaceSectorCount <= _spaceSectors.Count) break;

                    this.Remove(hash);
                }
            }
        }

        public void Lock(Hash hash)
        {
            lock (_lockObject)
            {
                int count;
                _lockedHashes.TryGetValue(hash, out count);

                count++;

                _lockedHashes[hash] = count;
            }
        }

        public void Unlock(Hash hash)
        {
            lock (_lockObject)
            {
                int count;
                if (!_lockedHashes.TryGetValue(hash, out count)) throw new KeyNotFoundException();

                count--;

                if (count == 0)
                {
                    _lockedHashes.Remove(hash);
                }
                else
                {
                    _lockedHashes[hash] = count;
                }
            }
        }

        public bool Contains(Hash hash)
        {
            lock (_lockObject)
            {
                return _clusterIndex.ContainsKey(hash) || _cacheInfoManager.Contains(hash);
            }
        }

        public IEnumerable<Hash> IntersectFrom(IEnumerable<Hash> collection)
        {
            lock (_lockObject)
            {
                foreach (var key in collection)
                {
                    if (this.Contains(key))
                    {
                        yield return key;
                    }
                }
            }
        }

        public IEnumerable<Hash> ExceptFrom(IEnumerable<Hash> collection)
        {
            lock (_lockObject)
            {
                foreach (var key in collection)
                {
                    if (!this.Contains(key))
                    {
                        yield return key;
                    }
                }
            }
        }

        public void Remove(Hash hash)
        {
            lock (_lockObject)
            {
                if (_clusterIndex.TryGetValue(hash, out var clusterInfo))
                {
                    _clusterIndex.Remove(hash);

                    if (_spaceSectors_Initialized)
                    {
                        foreach (long sector in clusterInfo.Indexes)
                        {
                            _bitmapManager.Set(sector, false);
                            if (_spaceSectors.Count < CacheManager.SpaceSectorCount) _spaceSectors.Add(sector);
                        }
                    }

                    // Event
                    _removedBlockEventQueue.Enqueue(hash);
                }
            }
        }

        public void Resize(long size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

            lock (_lockObject)
            {
                int unit = 1024 * 1024 * 256; // 256MB
                size = CacheManager.Roundup(size, unit);

                foreach (var key in _clusterIndex.Keys.ToArray()
                    .Where(n => _clusterIndex[n].Indexes.Any(point => size < (point * CacheManager.SectorSize) + CacheManager.SectorSize))
                    .ToArray())
                {
                    this.Remove(key);
                }

                _size = CacheManager.Roundup(size, CacheManager.SectorSize);
                _fileStream.SetLength(Math.Min(_size, _fileStream.Length));

                _spaceSectors.Clear();
                _spaceSectors_Initialized = false;
            }
        }

        public Task CheckBlocks(IProgress<CheckBlocksProgressReport> progress, CancellationToken token)
        {
            return Task.Run(() =>
            {
                // 読めないブロックを検出しRemoveする。

                var list = this.ToArray();

                int badCount = 0;
                int checkedCount = 0;
                int blockCount = list.Length;

                token.ThrowIfCancellationRequested();

                progress.Report(new CheckBlocksProgressReport(badCount, checkedCount, blockCount));

                foreach (var hash in list)
                {
                    token.ThrowIfCancellationRequested();

                    var buffer = new ArraySegment<byte>();

                    try
                    {
                        lock (_lockObject)
                        {
                            if (this.Contains(hash))
                            {
                                buffer = this[hash];
                            }
                        }
                    }
                    catch (Exception)
                    {
                        badCount++;
                    }
                    finally
                    {
                        if (buffer.Array != null)
                        {
                            _bufferManager.ReturnBuffer(buffer.Array);
                        }
                    }

                    checkedCount++;

                    if (checkedCount % 32 == 0)
                    {
                        progress.Report(new CheckBlocksProgressReport(badCount, checkedCount, blockCount));
                    }
                }

                progress.Report(new CheckBlocksProgressReport(badCount, checkedCount, blockCount));
            }, token);
        }

        private byte[] _sectorBuffer = new byte[CacheManager.SectorSize];

        public ArraySegment<byte> this[Hash hash]
        {
            get
            {
                // Cache
                {
                    ArraySegment<byte>? result = null;

                    lock (_lockObject)
                    {
                        if (_clusterIndex.TryGetValue(hash, out var clusterInfo))
                        {
                            clusterInfo.UpdateTime = DateTime.UtcNow;

                            var buffer = _bufferManager.TakeBuffer(clusterInfo.Length);

                            try
                            {
                                try
                                {
                                    for (int i = 0, remain = clusterInfo.Length; i < clusterInfo.Indexes.Length; i++, remain -= CacheManager.SectorSize)
                                    {
                                        long posision = clusterInfo.Indexes[i] * CacheManager.SectorSize;
                                        if (posision > _fileStream.Length) throw new ArgumentOutOfRangeException();

                                        if (_fileStream.Position != posision)
                                        {
                                            _fileStream.Seek(posision, SeekOrigin.Begin);
                                        }

                                        int length = Math.Min(remain, CacheManager.SectorSize);

                                        {
                                            _fileStream.Read(_sectorBuffer, 0, _sectorBuffer.Length);

                                            Unsafe.Copy(_sectorBuffer, 0, buffer, CacheManager.SectorSize * i, length);
                                        }
                                    }
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    throw new BlockNotFoundException();
                                }
                                catch (IOException)
                                {
                                    throw new BlockNotFoundException();
                                }

                                result = new ArraySegment<byte>(buffer, 0, clusterInfo.Length);
                            }
                            catch (Exception)
                            {
                                _bufferManager.ReturnBuffer(buffer);

                                this.Remove(hash);

                                throw;
                            }
                        }
                    }

                    if (result != null)
                    {
                        if (hash.Algorithm == HashAlgorithm.Sha256
                            && Unsafe.Equals(Sha256.ComputeHash(result.Value), hash.Value))
                        {
                            return result.Value;
                        }
                        else
                        {
                            _bufferManager.ReturnBuffer(result.Value.Array);

                            this.Remove(hash);
                        }
                    }
                }

                // Share
                {
                    ArraySegment<byte>? result = null;
                    ShareInfo shareInfo;

                    lock (_lockObject)
                    {
                        shareInfo = _cacheInfoManager.GetShareInfo(hash);

                        if (shareInfo != null)
                        {
                            var buffer = _bufferManager.TakeBuffer(shareInfo.BlockLength);

                            try
                            {
                                int length;

                                try
                                {
                                    using (var stream = new UnbufferedFileStream(shareInfo.Path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, _bufferManager))
                                    {
                                        stream.Seek((long)shareInfo.GetIndex(hash) * shareInfo.BlockLength, SeekOrigin.Begin);

                                        length = (int)Math.Min(stream.Length - stream.Position, shareInfo.BlockLength);
                                        stream.Read(buffer, 0, length);
                                    }
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    throw new BlockNotFoundException();
                                }
                                catch (IOException)
                                {
                                    throw new BlockNotFoundException();
                                }

                                result = new ArraySegment<byte>(buffer, 0, length);
                            }
                            catch (Exception)
                            {
                                _bufferManager.ReturnBuffer(buffer);

                                throw;
                            }
                        }
                    }

                    if (result != null)
                    {
                        if (hash.Algorithm == HashAlgorithm.Sha256
                            && Unsafe.Equals(Sha256.ComputeHash(result.Value), hash.Value))
                        {
                            return result.Value;
                        }
                        else
                        {
                            _bufferManager.ReturnBuffer(result.Value.Array);
                            result = null;

                            this.RemoveContent(shareInfo.Path);
                        }
                    }
                }

                throw new BlockNotFoundException();
            }
            set
            {
                if (value.Count > 1024 * 1024 * 32) throw new BadBlockException();

                if (hash.Algorithm == HashAlgorithm.Sha256)
                {
                    if (!Unsafe.Equals(Sha256.ComputeHash(value), hash.Value)) throw new BadBlockException();
                }
                else
                {
                    throw new FormatException();
                }

                lock (_lockObject)
                {
                    if (this.Contains(hash)) return;

                    List<long> sectorList = null;

                    try
                    {
                        int count = (value.Count + (CacheManager.SectorSize - 1)) / CacheManager.SectorSize;

                        sectorList = new List<long>(count);

                        if (_spaceSectors.Count < count)
                        {
                            this.CreatingSpace();
                        }

                        if (_spaceSectors.Count < count) throw new SpaceNotFoundException();

                        sectorList.AddRange(_spaceSectors.Take(count));

                        foreach (long sector in sectorList)
                        {
                            _bitmapManager.Set(sector, true);
                            _spaceSectors.Remove(sector);
                        }

                        for (int i = 0, remain = value.Count; i < sectorList.Count && 0 < remain; i++, remain -= CacheManager.SectorSize)
                        {
                            long posision = sectorList[i] * CacheManager.SectorSize;

                            if ((_fileStream.Length < posision + CacheManager.SectorSize))
                            {
                                int unit = 1024 * 1024 * 256; // 256MB
                                long size = CacheManager.Roundup((posision + CacheManager.SectorSize), unit);

                                _fileStream.SetLength(Math.Min(size, this.Size));
                            }

                            if (_fileStream.Position != posision)
                            {
                                _fileStream.Seek(posision, SeekOrigin.Begin);
                            }

                            int length = Math.Min(remain, CacheManager.SectorSize);

                            {
                                Unsafe.Copy(value.Array, value.Offset + (CacheManager.SectorSize * i), _sectorBuffer, 0, length);
                                Unsafe.Zero(_sectorBuffer, length, _sectorBuffer.Length - length);

                                _fileStream.Write(_sectorBuffer, 0, _sectorBuffer.Length);
                            }
                        }

                        _fileStream.Flush();
                    }
                    catch (SpaceNotFoundException e)
                    {
                        Log.Error(e);

                        throw e;
                    }
                    catch (IOException e)
                    {
                        Log.Error(e);

                        throw e;
                    }

                    var clusterInfo = new ClusterInfo(sectorList.ToArray(), value.Count);
                    clusterInfo.UpdateTime = DateTime.UtcNow;

                    _clusterIndex[hash] = clusterInfo;

                    // Event
                    _addedBlockEventQueue.Enqueue(hash);
                }
            }
        }

        public int GetLength(Hash hash)
        {
            lock (_lockObject)
            {
                if (_clusterIndex.ContainsKey(hash))
                {
                    return _clusterIndex[hash].Length;
                }

                // Share
                {
                    var shareInfo = _cacheInfoManager.GetShareInfo(hash);

                    if (shareInfo != null)
                    {
                        return Math.Min((int)(shareInfo.FileLength - (shareInfo.BlockLength * shareInfo.GetIndex(hash))), shareInfo.BlockLength);
                    }
                }

                return 0;
            }
        }

        public Stream Decoding(IEnumerable<Hash> hashes)
        {
            if (hashes == null) throw new ArgumentNullException(nameof(hashes));

            lock (_lockObject)
            {
                return new CacheStreamReader(hashes.ToList(), this, _bufferManager);
            }
        }

        private object _parityDecodingLockObject = new object();

        public Task<IEnumerable<Hash>> ParityDecoding(Group group, CancellationToken token)
        {
            return Task.Run<IEnumerable<Hash>>(() =>
            {
                lock (_parityDecodingLockObject)
                {
                    if (group.CorrectionAlgorithm == CorrectionAlgorithm.ReedSolomon8)
                    {
                        var hashList = group.Hashes.ToList();
                        int blockLength = group.Hashes.Max(n => this.GetLength(n));
                        int informationCount = hashList.Count / 2;

                        if (hashList.Take(informationCount).All(n => this.Contains(n)))
                        {
                            return hashList.Take(informationCount).ToList();
                        }

                        var buffers = new ArraySegment<byte>[informationCount];
                        var indexes = new int[informationCount];

                        try
                        {
                            // Load
                            {
                                int count = 0;

                                for (int i = 0; i < hashList.Count; i++)
                                {
                                    token.ThrowIfCancellationRequested();

                                    if (!this.Contains(hashList[i])) continue;

                                    var buffer = new ArraySegment<byte>();

                                    try
                                    {
                                        buffer = this[hashList[i]];

                                        if (buffer.Count < blockLength)
                                        {
                                            var tempBuffer = new ArraySegment<byte>(_bufferManager.TakeBuffer(blockLength), 0, blockLength);
                                            Unsafe.Copy(buffer.Array, buffer.Offset, tempBuffer.Array, tempBuffer.Offset, buffer.Count);
                                            Unsafe.Zero(tempBuffer.Array, tempBuffer.Offset + buffer.Count, tempBuffer.Count - buffer.Count);

                                            _bufferManager.ReturnBuffer(buffer.Array);

                                            buffer = tempBuffer;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        if (buffer.Array != null)
                                        {
                                            _bufferManager.ReturnBuffer(buffer.Array);
                                        }

                                        throw;
                                    }

                                    indexes[count] = i;
                                    buffers[count] = buffer;

                                    count++;

                                    if (count >= informationCount) break;
                                }

                                if (count < informationCount) throw new BlockNotFoundException();
                            }

                            using (var reedSolomon = new ReedSolomon8(informationCount, informationCount * 2, _threadCount, _bufferManager))
                            {
                                reedSolomon.Decode(buffers, indexes, blockLength, token).Wait();
                            }

                            // Set
                            {
                                long length = group.Length;

                                for (int i = 0; i < informationCount; length -= blockLength, i++)
                                {
                                    this[hashList[i]] = new ArraySegment<byte>(buffers[i].Array, buffers[i].Offset, (int)Math.Min(buffers[i].Count, length));
                                }
                            }
                        }
                        finally
                        {
                            foreach (var buffer in buffers)
                            {
                                if (buffer.Array == null) continue;

                                _bufferManager.ReturnBuffer(buffer.Array);
                            }
                        }

                        return hashList.Take(informationCount).ToList();
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            });
        }

        public Task<Metadata> Import(Stream stream, TimeSpan lifeSpan, CancellationToken token)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return Task.Run(() =>
            {
                Metadata metadata = null;
                var lockedHashes = new HashSet<Hash>();

                try
                {
                    const int blockLength = 1024 * 1024;
                    const HashAlgorithm hashAlgorithm = HashAlgorithm.Sha256;
                    const CorrectionAlgorithm correctionAlgorithm = CorrectionAlgorithm.ReedSolomon8;

                    int depth = 0;
                    var creationTime = DateTime.UtcNow;

                    var groupList = new List<Group>();

                    for (; ; )
                    {
                        if (stream.Length <= blockLength)
                        {
                            Hash hash = null;

                            using (var safeBuffer = _bufferManager.CreateSafeBuffer(blockLength))
                            {
                                int length = (int)stream.Length;
                                stream.Read(safeBuffer.Value, 0, length);

                                if (hashAlgorithm == HashAlgorithm.Sha256)
                                {
                                    hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(safeBuffer.Value, 0, length));
                                }

                                this.Lock(hash);
                                lockedHashes.Add(hash);

                                this[hash] = new ArraySegment<byte>(safeBuffer.Value, 0, length);
                            }

                            // Stream Dispose
                            {
                                stream.Dispose();
                                stream = null;
                            }

                            metadata = new Metadata(depth, hash);

                            break;
                        }
                        else
                        {
                            for (; ; )
                            {
                                var targetHashes = new List<Hash>();
                                var targetBuffers = new List<ArraySegment<byte>>();
                                long sumLength = 0;

                                try
                                {
                                    for (int i = 0; stream.Position < stream.Length; i++)
                                    {
                                        token.ThrowIfCancellationRequested();

                                        var buffer = new ArraySegment<byte>();

                                        try
                                        {
                                            int length = (int)Math.Min(stream.Length - stream.Position, blockLength);
                                            buffer = new ArraySegment<byte>(_bufferManager.TakeBuffer(length), 0, length);
                                            stream.Read(buffer.Array, 0, length);

                                            sumLength += length;
                                        }
                                        catch (Exception)
                                        {
                                            if (buffer.Array != null)
                                            {
                                                _bufferManager.ReturnBuffer(buffer.Array);
                                            }

                                            throw;
                                        }

                                        Hash hash;

                                        if (hashAlgorithm == HashAlgorithm.Sha256)
                                        {
                                            hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(buffer));
                                        }

                                        this.Lock(hash);
                                        lockedHashes.Add(hash);

                                        this[hash] = buffer;

                                        targetHashes.Add(hash);
                                        targetBuffers.Add(buffer);

                                        if (targetBuffers.Count >= 128) break;
                                    }

                                    var parityHashes = this.ParityEncoding(targetBuffers, hashAlgorithm, correctionAlgorithm, token);
                                    lockedHashes.UnionWith(parityHashes);

                                    groupList.Add(new Group(correctionAlgorithm, sumLength, CollectionUtils.Unite(targetHashes, parityHashes)));
                                }
                                finally
                                {
                                    foreach (var buffer in targetBuffers)
                                    {
                                        if (buffer.Array == null) continue;

                                        _bufferManager.ReturnBuffer(buffer.Array);
                                    }
                                }

                                if (stream.Position == stream.Length) break;
                            }

                            depth++;

                            // Stream Dispose
                            {
                                stream.Dispose();
                                stream = null;
                            }

                            stream = (new Index(groupList)).Export(_bufferManager);
                        }
                    }

                    lock (_lockObject)
                    {
                        if (!_cacheInfoManager.ContainsMessage(metadata))
                        {
                            _cacheInfoManager.Add(new CacheInfo(DateTime.UtcNow, lifeSpan, metadata, lockedHashes, null));

                            _importedBlockEventQueue.Enqueue(lockedHashes);
                        }
                    }

                    return metadata;
                }
                finally
                {
                    foreach (var hash in lockedHashes)
                    {
                        this.Unlock(hash);
                    }

                    if (stream != null)
                    {
                        stream.Dispose();
                        stream = null;
                    }
                }
            }, token);
        }

        public Task<Metadata> Import(string path, DateTime creationTime, CancellationToken token)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return Task.Run(() =>
            {
                // Check
                lock (_lockObject)
                {
                    var info = _cacheInfoManager.GetContentCacheInfo(path);
                    if (info != null) return info.Metadata;
                }

                Metadata metadata = null;
                var lockedHashes = new HashSet<Hash>();
                ShareInfo shareInfo = null;

                try
                {
                    const int blockLength = 1024 * 1024;
                    const HashAlgorithm hashAlgorithm = HashAlgorithm.Sha256;
                    const CorrectionAlgorithm correctionAlgorithm = CorrectionAlgorithm.ReedSolomon8;

                    int depth = 0;

                    var groupList = new List<Group>();

                    // File
                    using (var stream = new UnbufferedFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, _bufferManager))
                    {
                        if (stream.Length <= blockLength)
                        {
                            Hash hash = null;

                            using (var safeBuffer = _bufferManager.CreateSafeBuffer(blockLength))
                            {
                                int length = (int)stream.Length;
                                stream.Read(safeBuffer.Value, 0, length);

                                if (hashAlgorithm == HashAlgorithm.Sha256)
                                {
                                    hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(safeBuffer.Value, 0, length));
                                }
                            }

                            shareInfo = new ShareInfo(path, stream.Length, (int)stream.Length, new Hash[] { hash });
                            metadata = new Metadata(depth, hash);
                        }
                        else
                        {
                            var sharedHashes = new List<Hash>();

                            for (; ; )
                            {
                                var targetHashes = new List<Hash>();
                                var targetBuffers = new List<ArraySegment<byte>>();
                                long sumLength = 0;

                                try
                                {
                                    for (int i = 0; stream.Position < stream.Length; i++)
                                    {
                                        token.ThrowIfCancellationRequested();

                                        var buffer = new ArraySegment<byte>();

                                        try
                                        {
                                            int length = (int)Math.Min(stream.Length - stream.Position, blockLength);
                                            buffer = new ArraySegment<byte>(_bufferManager.TakeBuffer(length), 0, length);
                                            stream.Read(buffer.Array, 0, length);

                                            sumLength += length;
                                        }
                                        catch (Exception)
                                        {
                                            if (buffer.Array != null)
                                            {
                                                _bufferManager.ReturnBuffer(buffer.Array);
                                            }

                                            throw;
                                        }

                                        Hash hash;

                                        if (hashAlgorithm == HashAlgorithm.Sha256)
                                        {
                                            hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(buffer));
                                        }

                                        sharedHashes.Add(hash);

                                        targetHashes.Add(hash);
                                        targetBuffers.Add(buffer);

                                        if (targetBuffers.Count >= 128) break;
                                    }

                                    var parityHashes = this.ParityEncoding(targetBuffers, hashAlgorithm, correctionAlgorithm, token);
                                    lockedHashes.UnionWith(parityHashes);

                                    groupList.Add(new Group(correctionAlgorithm, sumLength, CollectionUtils.Unite(targetHashes, parityHashes)));
                                }
                                finally
                                {
                                    foreach (var buffer in targetBuffers)
                                    {
                                        if (buffer.Array == null) continue;

                                        _bufferManager.ReturnBuffer(buffer.Array);
                                    }
                                }

                                if (stream.Position == stream.Length) break;
                            }

                            shareInfo = new ShareInfo(path, stream.Length, blockLength, sharedHashes);

                            depth++;
                        }
                    }

                    while (groupList.Count > 0)
                    {
                        var index = new Index(groupList);
                        groupList.Clear();

                        // Index
                        using (var stream = index.Export(_bufferManager))
                        {
                            if (stream.Length <= blockLength)
                            {
                                Hash hash = null;

                                using (var safeBuffer = _bufferManager.CreateSafeBuffer(blockLength))
                                {
                                    int length = (int)stream.Length;
                                    stream.Read(safeBuffer.Value, 0, length);

                                    if (hashAlgorithm == HashAlgorithm.Sha256)
                                    {
                                        hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(safeBuffer.Value, 0, length));
                                    }

                                    this.Lock(hash);
                                    lockedHashes.Add(hash);

                                    this[hash] = new ArraySegment<byte>(safeBuffer.Value, 0, length);
                                }

                                metadata = new Metadata(depth, hash);
                            }
                            else
                            {
                                for (; ; )
                                {
                                    var targetHashes = new List<Hash>();
                                    var targetBuffers = new List<ArraySegment<byte>>();
                                    long sumLength = 0;

                                    try
                                    {
                                        for (int i = 0; stream.Position < stream.Length; i++)
                                        {
                                            token.ThrowIfCancellationRequested();

                                            var buffer = new ArraySegment<byte>();

                                            try
                                            {
                                                int length = (int)Math.Min(stream.Length - stream.Position, blockLength);
                                                buffer = new ArraySegment<byte>(_bufferManager.TakeBuffer(length), 0, length);
                                                stream.Read(buffer.Array, 0, length);

                                                sumLength += length;
                                            }
                                            catch (Exception)
                                            {
                                                if (buffer.Array != null)
                                                {
                                                    _bufferManager.ReturnBuffer(buffer.Array);
                                                }

                                                throw;
                                            }

                                            Hash hash;

                                            if (hashAlgorithm == HashAlgorithm.Sha256)
                                            {
                                                hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(buffer));
                                            }

                                            this.Lock(hash);
                                            lockedHashes.Add(hash);

                                            this[hash] = buffer;

                                            targetHashes.Add(hash);
                                            targetBuffers.Add(buffer);

                                            if (targetBuffers.Count >= 128) break;
                                        }

                                        var parityHashes = this.ParityEncoding(targetBuffers, hashAlgorithm, correctionAlgorithm, token);
                                        lockedHashes.UnionWith(parityHashes);

                                        groupList.Add(new Group(correctionAlgorithm, sumLength, CollectionUtils.Unite(targetHashes, parityHashes)));
                                    }
                                    finally
                                    {
                                        foreach (var buffer in targetBuffers)
                                        {
                                            if (buffer.Array == null) continue;

                                            _bufferManager.ReturnBuffer(buffer.Array);
                                        }
                                    }

                                    if (stream.Position == stream.Length) break;
                                }

                                depth++;
                            }
                        }
                    }

                    lock (_lockObject)
                    {
                        if (!_cacheInfoManager.ContainsContent(path))
                        {
                            _cacheInfoManager.Add(new CacheInfo(creationTime, Timeout.InfiniteTimeSpan, metadata, lockedHashes, shareInfo));

                            _importedBlockEventQueue.Enqueue(lockedHashes);
                        }
                    }

                    return metadata;
                }
                finally
                {
                    foreach (var hash in lockedHashes)
                    {
                        this.Unlock(hash);
                    }
                }
            }, token);
        }

        private object _parityEncodingLockObject = new object();

        private IEnumerable<Hash> ParityEncoding(IEnumerable<ArraySegment<byte>> buffers, HashAlgorithm hashAlgorithm, CorrectionAlgorithm correctionAlgorithm, CancellationToken token)
        {
            lock (_parityEncodingLockObject)
            {
                if (correctionAlgorithm == CorrectionAlgorithm.ReedSolomon8)
                {
                    if (buffers.Count() > 128) throw new ArgumentOutOfRangeException(nameof(buffers));

                    var createBuffers = new List<ArraySegment<byte>>();

                    try
                    {
                        var targetBuffers = new ArraySegment<byte>[buffers.Count()];
                        var parityBuffers = new ArraySegment<byte>[buffers.Count()];

                        int blockLength = buffers.Max(n => n.Count);

                        // Normalize
                        {
                            int index = 0;

                            foreach (var buffer in buffers)
                            {
                                token.ThrowIfCancellationRequested();

                                if (buffer.Count < blockLength)
                                {
                                    var tempBuffer = new ArraySegment<byte>(_bufferManager.TakeBuffer(blockLength), 0, blockLength);
                                    Unsafe.Copy(buffer.Array, buffer.Offset, tempBuffer.Array, tempBuffer.Offset, buffer.Count);
                                    Unsafe.Zero(tempBuffer.Array, tempBuffer.Offset + buffer.Count, tempBuffer.Count - buffer.Count);

                                    createBuffers.Add(tempBuffer);

                                    targetBuffers[index] = tempBuffer;
                                }
                                else
                                {
                                    targetBuffers[index] = buffer;
                                }

                                index++;
                            }
                        }

                        for (int i = 0; i < parityBuffers.Length; i++)
                        {
                            parityBuffers[i] = new ArraySegment<byte>(_bufferManager.TakeBuffer(blockLength), 0, blockLength);
                        }

                        var indexes = new int[parityBuffers.Length];

                        for (int i = 0; i < parityBuffers.Length; i++)
                        {
                            indexes[i] = targetBuffers.Length + i;
                        }

                        using (var reedSolomon = new ReedSolomon8(targetBuffers.Length, targetBuffers.Length + parityBuffers.Length, _threadCount, _bufferManager))
                        {
                            reedSolomon.Encode(targetBuffers, parityBuffers, indexes, blockLength, token).Wait();
                        }

                        token.ThrowIfCancellationRequested();

                        var parityHashes = new HashCollection();

                        for (int i = 0; i < parityBuffers.Length; i++)
                        {
                            Hash hash = null;

                            if (hashAlgorithm == HashAlgorithm.Sha256)
                            {
                                hash = new Hash(HashAlgorithm.Sha256, Sha256.ComputeHash(parityBuffers[i]));
                            }

                            this.Lock(hash);
                            this[hash] = parityBuffers[i];

                            parityHashes.Add(hash);
                        }

                        return parityHashes;
                    }
                    finally
                    {
                        foreach (var buffer in createBuffers)
                        {
                            if (buffer.Array == null) continue;

                            _bufferManager.ReturnBuffer(buffer.Array);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        #region Message

        private void CheckMessages()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                foreach (var info in _cacheInfoManager.GetMessageCacheInfos())
                {
                    if ((now - info.CreationTime) > info.LifeSpan)
                    {
                        _cacheInfoManager.RemoveMessage(info.Metadata);
                    }
                }
            }
        }

        #endregion

        #region Content

        private void CheckContents()
        {
            lock (_lockObject)
            {
                foreach (var cacheInfo in _cacheInfoManager.GetContentCacheInfos())
                {
                    if (cacheInfo.LockedHashes.All(n => this.Contains(n))) continue;

                    this.RemoveContent(cacheInfo.ShareInfo.Path);
                }
            }
        }

        public void RemoveContent(string path)
        {
            lock (_lockObject)
            {
                var cacheInfo = _cacheInfoManager.GetContentCacheInfo(path);
                if (cacheInfo == null) return;

                _cacheInfoManager.RemoveContent(path);

                // Event
                _removedBlockEventQueue.Enqueue(cacheInfo.ShareInfo.Hashes.Where(n => !this.Contains(n)).ToArray());
            }
        }

        public IEnumerable<Hash> GetContentHashes(string path)
        {
            lock (_lockObject)
            {
                var cacheInfo = _cacheInfoManager.GetContentCacheInfo(path);
                if (cacheInfo == null) Enumerable.Empty<Hash>();

                return cacheInfo.LockedHashes.ToArray();
            }
        }

        #endregion

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                _size = _settings.Load("Size", () => (long)1024 * 1024 * 1024 * 32);
                _clusterIndex = _settings.Load("ClusterIndex", () => new Dictionary<Hash, ClusterInfo>());

                foreach (var cacheInfo in _settings.Load<CacheInfo[]>("CacheInfos", () => new CacheInfo[0]))
                {
                    _cacheInfoManager.Add(cacheInfo);
                }

                _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 1, 0));
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("Size", _size);
                _settings.Save("ClusterIndex", _clusterIndex);
                _settings.Save("CacheInfos", _cacheInfoManager.ToArray());
            }
        }

        #endregion

        public Hash[] ToArray()
        {
            lock (_lockObject)
            {
                var hashSet = new HashSet<Hash>();
                hashSet.UnionWith(_clusterIndex.Keys);
                hashSet.UnionWith(_cacheInfoManager.GetHashes());

                return hashSet.ToArray();
            }
        }

        #region IEnumerable<Hash>

        public IEnumerator<Hash> GetEnumerator()
        {
            lock (_lockObject)
            {
                foreach (var hash in _clusterIndex.Keys)
                {
                    yield return hash;
                }
            }
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lockObject)
            {
                return this.GetEnumerator();
            }
        }

        #endregion

        [DataContract(Name = nameof(ClusterInfo))]
        private class ClusterInfo
        {
            private long[] _indexes;
            private int _length;
            private DateTime _updateTime;

            public ClusterInfo(long[] indexes, int length)
            {
                this.Indexes = indexes;
                this.Length = length;
            }

            [DataMember(Name = nameof(Indexes))]
            public long[] Indexes
            {
                get
                {
                    return _indexes;
                }
                private set
                {
                    _indexes = value;
                }
            }

            [DataMember(Name = nameof(Length))]
            public int Length
            {
                get
                {
                    return _length;
                }
                private set
                {
                    _length = value;
                }
            }

            [DataMember(Name = nameof(UpdateTime))]
            public DateTime UpdateTime
            {
                get
                {
                    return _updateTime;
                }
                set
                {
                    var utc = value.ToUniversalTime();
                    _updateTime = utc.AddTicks(-(utc.Ticks % TimeSpan.TicksPerSecond));
                }
            }
        }

        private class CacheInfoManager : IEnumerable<CacheInfo>
        {
            private Dictionary<Metadata, CacheInfo> _messageCacheInfos;
            private Dictionary<string, CacheInfo> _contentCacheInfos;

            private HashMap _hashMap;

            public CacheInfoManager()
            {
                _messageCacheInfos = new Dictionary<Metadata, CacheInfo>();
                _contentCacheInfos = new Dictionary<string, CacheInfo>();

                _hashMap = new HashMap();
            }

            public void Add(CacheInfo info)
            {
                if (info.ShareInfo == null)
                {
                    _messageCacheInfos.Add(info.Metadata, info);
                }
                else
                {
                    _contentCacheInfos.Add(info.ShareInfo.Path, info);

                    _hashMap.Add(info.ShareInfo);
                }
            }

            #region Message

            public void RemoveMessage(Metadata metadata)
            {
                _messageCacheInfos.Remove(metadata);
            }

            public bool ContainsMessage(Metadata metadata)
            {
                return _messageCacheInfos.ContainsKey(metadata);
            }

            public IEnumerable<CacheInfo> GetMessageCacheInfos()
            {
                return _messageCacheInfos.Values.ToArray();
            }

            public CacheInfo GetMessageCacheInfo(Metadata metadata)
            {
                CacheInfo cacheInfo;
                if (!_messageCacheInfos.TryGetValue(metadata, out cacheInfo)) return null;

                return cacheInfo;
            }

            #endregion

            #region Content

            public void RemoveContent(string path)
            {
                if (_contentCacheInfos.TryGetValue(path, out var cacheInfo))
                {
                    _contentCacheInfos.Remove(path);

                    _hashMap.Remove(cacheInfo.ShareInfo);
                }
            }

            public bool ContainsContent(string path)
            {
                return _contentCacheInfos.ContainsKey(path);
            }

            public IEnumerable<CacheInfo> GetContentCacheInfos()
            {
                return _contentCacheInfos.Values.ToArray();
            }

            public CacheInfo GetContentCacheInfo(string path)
            {
                CacheInfo cacheInfo;
                if (!_contentCacheInfos.TryGetValue(path, out cacheInfo)) return null;

                return cacheInfo;
            }

            #endregion

            #region Hash

            public bool Contains(Hash hash)
            {
                return _hashMap.Contains(hash);
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

            #region IEnumerable<CacheInfo>

            public IEnumerator<CacheInfo> GetEnumerator()
            {
                foreach (var info in CollectionUtils.Unite(_messageCacheInfos.Values, _contentCacheInfos.Values))
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

            private class HashMap
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

        [DataContract(Name = nameof(CacheInfo))]
        private class CacheInfo
        {
            private DateTime _creationTime;
            private TimeSpan _lifeSpan;

            private Metadata _metadata;
            private HashCollection _lockedHashes;
            private ShareInfo _shareInfo;

            public CacheInfo(DateTime creationTime, TimeSpan lifeTime, Metadata metadata, IEnumerable<Hash> lockedHashes, ShareInfo shareInfo)
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
        private class ShareInfo
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _addedBlockEventQueue.Dispose();
                _removedBlockEventQueue.Dispose();

                if (_fileStream != null)
                {
                    try
                    {
                        _fileStream.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _fileStream = null;
                }

                if (_watchTimer != null)
                {
                    try
                    {
                        _watchTimer.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _watchTimer = null;
                }
            }
        }
    }

    class CacheManagerException : ManagerException
    {
        public CacheManagerException() : base() { }
        public CacheManagerException(string message) : base(message) { }
        public CacheManagerException(string message, Exception innerException) : base(message, innerException) { }
    }

    class SpaceNotFoundException : CacheManagerException
    {
        public SpaceNotFoundException() : base() { }
        public SpaceNotFoundException(string message) : base(message) { }
        public SpaceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    class BlockNotFoundException : CacheManagerException
    {
        public BlockNotFoundException() : base() { }
        public BlockNotFoundException(string message) : base(message) { }
        public BlockNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    class BadBlockException : CacheManagerException
    {
        public BadBlockException() : base() { }
        public BadBlockException(string message) : base(message) { }
        public BadBlockException(string message, Exception innerException) : base(message, innerException) { }
    }
}

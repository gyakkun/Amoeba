using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Omnius.Base;

namespace Amoeba.Core
{
    public class RouteTable<T> : IEnumerable<Node<T>>, IEnumerable
    {
        private int _row;
        private int _col;
        private byte[] _baseId;

        private LinkedList<Node<T>>[] _nodesList;

        private readonly object _lockObject = new object();

        private static byte[] _distanceHashtable = new byte[256];

        static RouteTable()
        {
            _distanceHashtable[0] = 0;
            _distanceHashtable[1] = 1;

            int i = 2;

            for (; i < 0x4; i++) _distanceHashtable[i] = 2;
            for (; i < 0x8; i++) _distanceHashtable[i] = 3;
            for (; i < 0x10; i++) _distanceHashtable[i] = 4;
            for (; i < 0x20; i++) _distanceHashtable[i] = 5;
            for (; i < 0x40; i++) _distanceHashtable[i] = 6;
            for (; i < 0x80; i++) _distanceHashtable[i] = 7;
            for (; i <= 0xff; i++) _distanceHashtable[i] = 8;
        }

        public RouteTable(int row, int column)
        {
            if (row <= 0) throw new ArgumentOutOfRangeException(nameof(row));
            if (column <= 0) throw new ArgumentOutOfRangeException(nameof(column));

            _row = row;
            _col = column;
            _nodesList = new LinkedList<Node<T>>[row];
        }

        public byte[] BaseId
        {
            get
            {
                lock (_lockObject)
                {
                    return _baseId;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    var tempList = this.ToArray();
                    this.Clear();

                    _baseId = value;

                    foreach (var item in tempList)
                    {
                        this.Add(item.Id, item.Value);
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _nodesList
                        .Where(n => n != null)
                        .Sum(m => m.Count);
                }
            }
        }

        private static int Distance(byte[] x, byte[] y)
        {
            int result = 0;

            int length = Math.Min(x.Length, y.Length);

            for (int i = 0; i < length; i++)
            {
                byte value = (byte)(x[i] ^ y[i]);

                result = _distanceHashtable[value];

                if (result != 0)
                {
                    result += (length - (i + 1)) * 8;

                    break;
                }
            }

            return result;
        }

        public static IEnumerable<Node<T>> Search(byte[] targetId, byte[] baseId, IEnumerable<Node<T>> nodeList, int count)
        {
            if (targetId == null) throw new ArgumentNullException(nameof(targetId));
            if (nodeList == null) throw new ArgumentNullException(nameof(nodeList));
            if (count == 0) return new Node<T>[0];

            var targetList = new List<SortInfo>();

            if (baseId != null)
            {
                var xor = new byte[targetId.Length];
                Unsafe.Xor(targetId, baseId, xor);
                targetList.Add(new SortInfo(null, xor));
            }

            foreach (var node in nodeList)
            {
                var xor = new byte[targetId.Length];
                Unsafe.Xor(targetId, node.Id, xor);
                targetList.Add(new SortInfo(node, xor));
            }

            for (int i = 1; i < targetList.Count; i++)
            {
                var temp = targetList[i];

                int left = 0;
                int right = Math.Min(i, count);

                while (left < right)
                {
                    int middle = (left + right) / 2;

                    if (Unsafe.Compare(targetList[middle].Xor, temp.Xor) <= 0)
                    {
                        left = middle + 1;
                    }
                    else
                    {
                        right = middle;
                    }
                }

                for (int j = Math.Min(i, count); left < j; --j)
                {
                    targetList[j] = targetList[j - 1];
                }

                targetList[left] = temp;
            }

            return targetList.Take(count).TakeWhile(n => n.Node.HasValue).Select(n => n.Node.Value).ToList();
        }

        private struct SortInfo
        {
            public SortInfo(Node<T>? node, byte[] xor)
            {
                this.Node = node;
                this.Xor = xor;
            }

            public Node<T>? Node { get; private set; }
            public byte[] Xor { get; private set; }
        }

        public bool Add(byte[] id, T value)
        {
            lock (_lockObject)
            {
                if (id == null) throw new ArgumentNullException(nameof(id));

                int i = RouteTable<T>.Distance(this.BaseId, id) - 1;
                if (i == -1) return false;

                var targetList = _nodesList[i];

                // 生存率の高いNodeはFirstに、そうでないNodeはLastに
                if (targetList != null)
                {
                    if (!targetList.Any(n => Unsafe.Equals(n.Id, id)))
                    {
                        if (targetList.Count < _col)
                        {
                            targetList.AddFirst(new Node<T>(id, value));

                            return true;
                        }
                    }
                }
                else
                {
                    targetList = new LinkedList<Node<T>>();
                    targetList.AddFirst(new Node<T>(id, value));
                    _nodesList[i] = targetList;

                    return true;
                }

                return false;
            }
        }

        public IEnumerable<Node<T>> Search(byte[] targetId, int count)
        {
            lock (_lockObject)
            {
                if (targetId == null) throw new ArgumentNullException(nameof(targetId));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

                return RouteTable<T>.Search(targetId, _baseId, this.ToArray(), count);
            }
        }

        public Node<T> Verify()
        {
            lock (_lockObject)
            {
                for (int i = _nodesList.Length - 1; i >= 0; i--)
                {
                    if (_nodesList[i] != null)
                    {
                        if (_nodesList[i].Count == _col)
                        {
                            return _nodesList[i].Last.Value;
                        }
                    }
                }

                return default(Node<T>);
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _nodesList.Length; i++)
                {
                    _nodesList[i] = null;
                }
            }
        }

        public bool Contains(byte[] id)
        {
            lock (_lockObject)
            {
                if (id == null) throw new ArgumentNullException(nameof(id));

                int i = RouteTable<T>.Distance(this.BaseId, id) - 1;
                if (i == -1) return false;

                var targetList = _nodesList[i];

                if (targetList != null)
                {
                    return targetList.Any((item) => Unsafe.Equals(item.Id, id));
                }

                return false;
            }
        }

        public Node<T> Get(byte[] id)
        {
            lock (_lockObject)
            {
                if (id == null) throw new ArgumentNullException(nameof(id));

                int i = RouteTable<T>.Distance(this.BaseId, id) - 1;
                if (i == -1) return default(Node<T>);

                var targetList = _nodesList[i];

                if (targetList != null)
                {
                    return targetList.FirstOrDefault((item) => Unsafe.Equals(item.Id, id));
                }

                return default(Node<T>);
            }
        }

        public bool Remove(byte[] id)
        {
            lock (_lockObject)
            {
                if (id == null) throw new ArgumentNullException(nameof(id));

                int i = RouteTable<T>.Distance(this.BaseId, id) - 1;
                if (i == -1) return false;

                var targetList = _nodesList[i];

                if (targetList != null)
                {
                    return targetList.RemoveAll((item) => Unsafe.Equals(item.Id, id)) != 0;
                }

                return false;
            }
        }

        public Node<T>[] ToArray()
        {
            lock (_lockObject)
            {
                var tempList = new List<Node<T>>();

                for (int i = 0; i < _nodesList.Length; i++)
                {
                    if (_nodesList[i] != null)
                    {
                        tempList.AddRange(_nodesList[i]);
                    }
                }

                return tempList.ToArray();
            }
        }

        public IEnumerator<Node<T>> GetEnumerator()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _nodesList.Length; i++)
                {
                    if (_nodesList[i] != null)
                    {
                        foreach (var node in _nodesList[i].ToArray())
                        {
                            yield return node;
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lockObject)
            {
                return this.GetEnumerator();
            }
        }
    }
}

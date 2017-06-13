using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;

namespace Amoeba.Service
{
    sealed class PriorityManager
    {
        private Dictionary<DateTime, int> _table = new Dictionary<DateTime, int>();

        private readonly TimeSpan _survivalTime;

        private readonly object _lockObject = new object();

        public PriorityManager(TimeSpan survivalTime)
        {
            _survivalTime = survivalTime;
        }

        public TimeSpan SurvivalTime
        {
            get
            {
                return _survivalTime;
            }
        }

        public void Increment()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));

                _table.AddOrUpdate(now, 1, (_, origin) => origin + 1);
            }
        }

        public double GetPriority()
        {
            const int min = 32;
            const int max = 256;

            lock (_lockObject)
            {
                int priority = _table.Sum(n => n.Value);
                priority = Math.Min(Math.Max(priority, min), max);

                return ((double)priority) / max;
            }
        }

        public void Update()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                foreach (var key in _table.Keys.ToArray())
                {
                    if ((now - key) < _survivalTime) continue;

                    _table.Remove(key);
                }
            }
        }
    }
}

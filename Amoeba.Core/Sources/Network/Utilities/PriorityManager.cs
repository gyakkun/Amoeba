using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;

namespace Amoeba.Core
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
                now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));

                _table.AddOrUpdate(now, 1, (_, origin) => origin + 1);
            }
        }

        public void Decrement()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));

                _table.AddOrUpdate(now, -1, (_, origin) => origin - 1);
            }
        }

        public double GetPriority()
        {
            const int average = 256;

            lock (_lockObject)
            {
                var priority = _table.Sum(n => n.Value);
                priority = Math.Min(Math.Max(priority, -average), average);

                return ((double)(priority + average)) / (average * 2);
            }
        }

        public void Update()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;

                foreach (var key in _table.Keys.ToArray())
                {
                    if ((now - key) > _survivalTime) continue;

                    _table.Remove(key);
                }
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Windows;
using Library;
using Library.Collections;
using Library.Net.Amoeba;
using Library.Utilities;

namespace Amoeba
{
    sealed class SeedStateCache
    {
        private AmoebaManager _amoebaManager;
        private ConcurrentDictionary<Seed, SearchState> _seedsDictionary = new ConcurrentDictionary<Seed, SearchState>();
        private readonly object _thisLock = new object();

        private WatchTimer _watchTimer;

        public SeedStateCache(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _watchTimer = new WatchTimer(this.WatchTimer, 0, 1000 * 60);
        }

        private void WatchTimer()
        {
            try
            {
                var tempDictionary = new ConcurrentDictionary<Seed, SearchState>();

                {
                    foreach (var seed in _amoebaManager.CacheSeeds)
                    {
                        tempDictionary[seed] = SearchState.Cache;
                    }

                    foreach (var information in _amoebaManager.UploadingInformation)
                    {
                        if (information.Contains("Seed") && ((UploadState)information["State"]) != UploadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];

                            tempDictionary.AddOrUpdate(seed, SearchState.Uploading, (_, orignalState) => orignalState | SearchState.Uploading);
                        }
                    }

                    foreach (var information in _amoebaManager.DownloadingInformation)
                    {
                        if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];

                            tempDictionary.AddOrUpdate(seed, SearchState.Downloading, (_, orignalState) => orignalState | SearchState.Downloading);
                        }
                    }

                    foreach (var seed in _amoebaManager.UploadedSeeds)
                    {
                        tempDictionary.AddOrUpdate(seed, SearchState.Uploaded, (_, orignalState) => orignalState | SearchState.Uploaded);
                    }

                    foreach (var seed in _amoebaManager.DownloadedSeeds)
                    {
                        tempDictionary.AddOrUpdate(seed, SearchState.Downloaded, (_, orignalState) => orignalState | SearchState.Downloaded);
                    }
                }

                lock (_thisLock)
                {
                    _seedsDictionary = tempDictionary;
                }
            }
            catch (Exception)
            {

            }
        }

        public SearchState GetState(Seed seed)
        {
            lock (_thisLock)
            {
                SearchState state;
                _seedsDictionary.TryGetValue(seed, out state);

                return state;
            }
        }

        public void SetState(Seed seed, SearchState state)
        {
            lock (_thisLock)
            {
                _seedsDictionary.AddOrUpdate(seed, state, (_, orignalState) => orignalState | state);
            }
        }
    }
}

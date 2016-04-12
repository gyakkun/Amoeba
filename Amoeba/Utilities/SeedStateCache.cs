using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Windows;
using Library.Collections;
using Library.Net.Amoeba;

namespace Amoeba
{
    sealed class SeedStateCache
    {
        private AmoebaManager _amoebaManager;
        private Dictionary<Seed, SearchState> _seedsDictionary = new Dictionary<Seed, SearchState>();
        private readonly object _thisLock = new object();

        private System.Threading.Timer _watchTimer;
        private volatile bool _isRefreshing = false;

        public SeedStateCache(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _watchTimer = new Timer(this.WatchTimer, null, 0, 1000 * 60);
        }

        private void WatchTimer(object state)
        {
            this.Refresh();
        }

        private void Refresh()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                var tempDictionary = new Dictionary<Seed, SearchState>();

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
                            SearchState state;

                            if (tempDictionary.TryGetValue(seed, out state))
                            {
                                state |= SearchState.Uploading;
                                tempDictionary[seed] = state;
                            }
                            else
                            {
                                tempDictionary.Add(seed, SearchState.Uploading);
                            }
                        }
                    }

                    foreach (var information in _amoebaManager.DownloadingInformation)
                    {
                        if (information.Contains("Seed") && ((DownloadState)information["State"]) != DownloadState.Completed)
                        {
                            var seed = (Seed)information["Seed"];
                            SearchState state;

                            if (tempDictionary.TryGetValue(seed, out state))
                            {
                                state |= SearchState.Downloading;
                                tempDictionary[seed] = state;
                            }
                            else
                            {
                                tempDictionary.Add(seed, SearchState.Downloading);
                            }
                        }
                    }

                    foreach (var seed in _amoebaManager.UploadedSeeds)
                    {
                        SearchState state;

                        if (tempDictionary.TryGetValue(seed, out state))
                        {
                            state |= SearchState.Uploaded;
                            tempDictionary[seed] = state;
                        }
                        else
                        {
                            tempDictionary.Add(seed, SearchState.Uploaded);
                        }
                    }

                    foreach (var seed in _amoebaManager.DownloadedSeeds)
                    {
                        SearchState state;

                        if (tempDictionary.TryGetValue(seed, out state))
                        {
                            state |= SearchState.Downloaded;
                            tempDictionary[seed] = state;
                        }
                        else
                        {
                            tempDictionary.Add(seed, SearchState.Downloaded);
                        }
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
            finally
            {
                _isRefreshing = false;
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
                SearchState orignalState;

                if (_seedsDictionary.TryGetValue(seed, out orignalState))
                {
                    _seedsDictionary[seed] = (orignalState | state);
                }
                else
                {
                    _seedsDictionary.Add(seed, state);
                }
            }
        }
    }
}

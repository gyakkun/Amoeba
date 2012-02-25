using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;
using Library.Net.Amoeba;
using Amoeba.Properties;
using System.ComponentModel;
using Library.Collections;
using System.Threading;
using Library;
using System.Windows.Input;
using System.Windows.Threading;

namespace Amoeba.Windows
{
    /// <summary>
    /// SearchControl.xaml の相互作用ロジック
    /// </summary>
    partial class SearchControl : UserControl
    {
        private BufferManager _bufferManager;
        private AmoebaManager _amoebaManager;

        private SearchTreeViewItem _searchTreeViewItem;
        private List<SearchListViewItem> _searchListViewItemCollection;

        public SearchControl(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _amoebaManager = amoebaManager;

            _searchListViewItemCollection = new List<SearchListViewItem>();

            InitializeComponent();

            _searchListView.ItemsSource = _searchListViewItemCollection;

            _searchTreeViewItem = new SearchTreeViewItem(Settings.Instance.SearchControl_SearchTreeItem);

            try
            {
                _searchTreeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _searchTreeView.Items.Add(_searchTreeViewItem);

            _amoebaManager.GetFilterSeedEvent = (object sender, Seed seed) =>
            {
                var searchItem = new SearchListViewItem();

                searchItem.Name = seed.Name;
                searchItem.Signature = MessageConverter.ToSignatureString(seed.Certificate);
                searchItem.Keywords = seed.Keywords.Where(n => n != null || n.Value != null).Select(m => m.Value);
                searchItem.CreationTime = seed.CreationTime;
                searchItem.Length = seed.Length;
                searchItem.Comment = seed.Comment;
                searchItem.Value = seed;

                HashSet<SearchListViewItem> searchItems = new HashSet<SearchListViewItem>();
                searchItems.Add(searchItem);

                SearchControl.Filter(ref searchItems, _searchTreeViewItem.Value);

                return searchItems.Count == 0 ? false : true;
            };

            this.Sort();
        }

        private IEnumerable<SearchListViewItem> Searching()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                Dictionary<SearchState, HashSet<Seed>> seedsDictionary = new Dictionary<SearchState, HashSet<Seed>>();

                seedsDictionary.Add(SearchState.Searching, new HashSet<Seed>());
                seedsDictionary.Add(SearchState.Downloading, new HashSet<Seed>());
                seedsDictionary.Add(SearchState.Downloaded, new HashSet<Seed>());
                seedsDictionary.Add(SearchState.Uploading, new HashSet<Seed>());
                seedsDictionary.Add(SearchState.Uploaded, new HashSet<Seed>());

                using (DeadlockMonitor.Lock(_amoebaManager.ThisLock))
                {
                    foreach (var information in _amoebaManager.DownloadingInformation)
                    {
                        if (information.Contains("Seed"))
                        {
                            seedsDictionary[SearchState.Downloading].Add((Seed)information["Seed"]);
                        }
                    }

                    foreach (var information in _amoebaManager.DownloadedSeeds)
                    {
                        seedsDictionary[SearchState.Downloaded].Add(information);
                    }
                
                    foreach (var information in _amoebaManager.UploadingInformation)
                    {
                        if (information.Contains("Seed"))
                        {
                            seedsDictionary[SearchState.Uploading].Add((Seed)information["Seed"]);
                        }
                    }

                    foreach (var information in _amoebaManager.UploadedSeeds)
                    {
                        seedsDictionary[SearchState.Uploaded].Add(information);
                    }
                
                    foreach (var seed in _amoebaManager.Seeds)
                    {
                        seedsDictionary[SearchState.Searching].Add(seed);
                    }
                }

                SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                foreach (var state in new SearchState[] { SearchState.Searching, SearchState.Downloading, SearchState.Downloaded, SearchState.Uploading, SearchState.Uploaded })
                {
                    foreach (var state2 in new SearchState[] { SearchState.Searching, SearchState.Downloading, SearchState.Downloaded, SearchState.Uploading, SearchState.Uploaded })
                    {
                        if (state == state2) continue;

                        foreach (var item in seedsDictionary[state])
                        {
                            if (seedsDictionary[state2].Any(n => comparer.Equals(n, item)))
                            {
                                seedsDictionary[state2].Add(item);
                            }
                        }
                    }
                }

                Dictionary<Seed, SearchListViewItem> searchItems = new Dictionary<Seed, SearchListViewItem>(new SeedEqualityComparer());

                foreach (var item in seedsDictionary)
                {
                    foreach (var seed in item.Value)
                    {
                        SearchState searchState = item.Key;

                        if (searchItems.ContainsKey(seed) && searchItems[seed].CreationTime < seed.CreationTime)
                        {
                            searchState |= searchItems[seed].State;
                            searchItems.Remove(seed);
                        }

                        if (!searchItems.ContainsKey(seed))
                        {
                            var searchItem = new SearchListViewItem();

                            searchItem.Name = seed.Name;
                            searchItem.Signature = MessageConverter.ToSignatureString(seed.Certificate);
                            searchItem.Keywords = seed.Keywords.Where(n => n != null || n.Value != null).Select(m => m.Value);
                            searchItem.CreationTime = seed.CreationTime;
                            searchItem.Length = seed.Length;
                            searchItem.Comment = seed.Comment;
                            searchItem.Value = seed;

                            searchItems.Add(seed, searchItem);
                        }

                        searchItems[seed].State |= searchState;
                    }
                }

                sw.Stop();
                Debug.WriteLine(string.Format("Searching Time: {0}", sw.ElapsedMilliseconds));

                return searchItems.Values;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return new SearchListViewItem[0] { };
        }

        private void Update()
        {
            Settings.Instance.SearchControl_SearchTreeItem = _searchTreeViewItem.Value;

            _searchTreeView_SelectedItemChanged(this, null);
        }

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
                if (selectSearchTreeViewItem == null) return;
                if (_searchTextBox.Text == "") return;

                var searchTreeItem = new SearchTreeItem();
                searchTreeItem.SearchItem = new SearchItem();
                searchTreeItem.SearchItem.Name = _searchTextBox.Text;
                searchTreeItem.SearchItem.SearchNameCollection.Add(new SearchContains<string>() { Contains = true, Value = _searchTextBox.Text });

                selectSearchTreeViewItem.Value.Items.Add(searchTreeItem);

                selectSearchTreeViewItem.Update();

                _searchTextBox.Text = "";
            }
        }

        #region _searchListView

        private void _searchListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_searchListView.GetCurrentIndex(e.GetPosition) < 0) return;

            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value);
            }
        }

        private void _searchListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_searchListViewCopyMenuItem != null) _searchListViewCopyMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewCopyInfoMenuItem != null) _searchListViewCopyInfoMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewDownloadHistoryDeleteMenuItem != null) _searchListViewDownloadHistoryDeleteMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewUploadHistoryDeleteMenuItem != null) _searchListViewUploadHistoryDeleteMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewFilterNameMenuItem != null) _searchListViewFilterNameMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewFilterSignatureMenuItem != null) _searchListViewFilterSignatureMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewFilterSeedMenuItem != null) _searchListViewFilterSeedMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
            if (_searchListViewDownloadMenuItem != null) _searchListViewDownloadMenuItem.IsEnabled = (_searchListView.SelectedItems.Count > 0);
        }

        private void _searchListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var sb = new StringBuilder();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                sb.AppendLine(AmoebaConverter.ToSeedString(item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _searchListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            var item = selectSearchListViewItems.Cast<SearchListViewItem>().FirstOrDefault();
            if (item == null) return;

            try
            {
                Clipboard.SetText(MessageConverter.ToInfoMessage(item.Value));
            }
            catch (Exception)
            {

            }
        }

        private void _searchListViewDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                _amoebaManager.Download(item.Value);
            }
        }

        private void _searchListViewFilterNameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Name == null || item.Name == "") continue;

                _searchTreeViewItem.Value.SearchItem.SearchNameCollection.Add(
                    new SearchContains<string>() { Contains = false, Value = item.Name });
            }

            this.Update();
        }

        private void _searchListViewFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Signature == null || item.Signature == "") continue;

                _searchTreeViewItem.Value.SearchItem.SearchSignatureCollection.Add(
                    new SearchContains<string>() { Contains = false, Value = item.Signature });
            }

            this.Update();
        }

        private void _searchListViewFilterSeedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null) continue;

                _searchTreeViewItem.Value.SearchItem.SearchSeedCollection.Add(
                    new SearchContains<Seed>() { Contains = false, Value = item.Value });
            }

            this.Update();
        }

        private void _searchListViewDownloadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null) continue;

                foreach (var seed in _amoebaManager.DownloadedSeeds.ToArray())
                {
                    if (comparer.Equals(item.Value, seed))
                    {
                        _amoebaManager.DownloadedSeeds.Remove(seed);
                    }
                }
            }

            this.Update();
        }

        private void _searchListViewUploadHistoryDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchListViewItems = _searchListView.SelectedItems;
            if (selectSearchListViewItems == null) return;

            SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

            foreach (var item in selectSearchListViewItems.Cast<SearchListViewItem>())
            {
                if (item.Value == null) continue;

                foreach (var seed in _amoebaManager.UploadedSeeds.ToArray())
                {
                    if (comparer.Equals(item.Value, seed))
                    {
                        _amoebaManager.UploadedSeeds.Remove(seed);
                    }
                }
            }

            this.Update();
        }

        #endregion

        #region _searchTreeView

        private void _searchTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this._searchTreeView_SelectedItemChanged(null, null);
        }

        private void _searchTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null)
                return;

            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(delegate(object state2)
            {
                HashSet<SearchListViewItem> searchListViewItems = new HashSet<SearchListViewItem>();

                searchListViewItems.UnionWith(this.Searching());

                foreach (var searchTreeViewItem in _searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>())
                {
                    SearchControl.Filter(ref searchListViewItems, searchTreeViewItem.Value);
                    searchTreeViewItem.Hit = searchListViewItems.Count;
                }

                _searchListViewItemCollection.Clear();
                _searchListViewItemCollection.AddRange(searchListViewItems);

                this.Sort();
            }), null);
        }

        private static void Filter(ref HashSet<SearchListViewItem> searchItems, SearchTreeItem searchTreeItem)
        {
            searchItems.IntersectWith(searchItems.ToArray().Where(searchItem =>
            {
                bool flag;

                if (searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.Verify(searchItem.Length);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.Verify(searchItem.CreationTime);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchKeywordCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchKeywordCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchItem.Keywords.Contains(searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchItem.Signature == searchContains.Value;

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchNameCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchNameCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains)
                        {
                            return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => searchItem.Name.Contains(n));
                        }

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == true))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains)
                        {
                            Regex regex = new Regex(searchContains.Value);
                            return regex.IsMatch(searchItem.Name);
                        }

                        return false;
                    });
                    if (!flag) return false;
                }

                if (searchTreeItem.SearchItem.SearchSeedCollection.Any(n => n.Contains == true))
                {
                    SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                    flag = searchTreeItem.SearchItem.SearchSeedCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return comparer.Equals(searchItem.Value, searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

                return true;
            }));

            searchItems.ExceptWith(searchItems.ToArray().Where(searchItem =>
            {
                bool flag;

                if (searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchLengthRangeCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.Verify(searchItem.Length);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.Verify(searchItem.CreationTime);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchKeywordCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchKeywordCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchItem.Keywords.Contains(searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchSignatureCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchSignatureCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchItem.Signature == searchContains.Value;

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchNameCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchNameCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains)
                        {
                            return searchContains.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(n => searchItem.Name.Contains(n));
                        }

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchNameRegexCollection.Any(n => n.Contains == false))
                {
                    flag = searchTreeItem.SearchItem.SearchNameRegexCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains)
                        {
                            Regex regex = new Regex(searchContains.Value);
                            return regex.IsMatch(searchItem.Name);
                        }

                        return false;
                    });
                    if (flag) return true;
                }

                if (searchTreeItem.SearchItem.SearchSeedCollection.Any(n => n.Contains == false))
                {
                    SeedHashEqualityComparer comparer = new SeedHashEqualityComparer();

                    flag = searchTreeItem.SearchItem.SearchSeedCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return comparer.Equals(searchItem.Value, searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

                return false;
            }));
        }

        private void _searchTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            _searchTreeViewDeleteMenuItem.IsEnabled = !(selectSearchTreeViewItem == _searchTreeViewItem);
            _searchTreeViewCutContextMenuItem.IsEnabled = !(selectSearchTreeViewItem == _searchTreeViewItem);
        }

        private void _searchTreeViewAddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            var searchTreeItem = new SearchTreeItem();
            searchTreeItem.SearchItem = new SearchItem();

            var searchItem = searchTreeItem.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);

            if (true == window.ShowDialog())
            {
                selectSearchTreeViewItem.IsExpanded = true;
                selectSearchTreeViewItem.Value.Items.Add(searchTreeItem);

                selectSearchTreeViewItem.Update();
            }

            this.Update();
        }

        private void _searchTreeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            var searchItem = selectSearchTreeViewItem.Value.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.ShowDialog();

            selectSearchTreeViewItem.Update();

            this.Update();
        }

        private void _searchTreeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            if (selectSearchTreeViewItem == _searchTreeViewItem) return;

            var list = _searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>().ToList();
            list[list.Count - 2].Value.Items.Remove(selectSearchTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _searchTreeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectSearchTreeViewItem.Value.DeepClone() });

            var list = _searchTreeViewItem.GetLineage(selectSearchTreeViewItem).OfType<SearchTreeViewItem>().ToList();
            list[list.Count - 2].Value.Items.Remove(selectSearchTreeViewItem.Value);
            list[list.Count - 2].Update();

            this.Update();
        }

        private void _searchTreeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new List<SearchTreeItem>() { selectSearchTreeViewItem.Value.DeepClone() });
        }

        private void _searchTreeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectSearchTreeViewItem = _searchTreeView.SelectedItem as SearchTreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            foreach (var searchTreeitem in Clipboard.GetSearchTreeItems().Select(n => n.DeepClone()))
            {
                selectSearchTreeViewItem.Value.Items.Add(searchTreeitem);
            }

            selectSearchTreeViewItem.Update();

            this.Update();
        }

        #endregion

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private string _lastHeaderClicked = LanguagesManager.Instance.SearchControl_Name;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                _searchListView.SelectedIndex = -1;

                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != _lastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (_lastDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
            else
            {
                if (_lastHeaderClicked != null)
                {
                    Sort(_lastHeaderClicked, _lastDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
            {
                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            if (sortBy == LanguagesManager.Instance.SearchControl_Name)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    string xName = x.Name == null ? "" : x.Name;
                    string yName = y.Name == null ? "" : y.Name;

                    return xName.CompareTo(yName);
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Signature)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    string xSignature = x.Signature == null ? "" : x.Signature;
                    string ySignature = y.Signature == null ? "" : y.Signature;

                    return xSignature.CompareTo(ySignature);
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Length)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    return x.Length.CompareTo(y.Length);
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Keywords)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    StringBuilder xBuilder = new StringBuilder();
                    foreach (var item in x.Keywords) xBuilder.Append(item);
                    StringBuilder yBuilder = new StringBuilder();
                    foreach (var item in y.Keywords) yBuilder.Append(item);

                    return xBuilder.ToString().CompareTo(yBuilder.ToString());
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_CreationTime)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    return x.CreationTime.CompareTo(y.CreationTime);
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_Comment)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    string xComment = x.Comment == null ? "" : x.Comment;
                    string yComment = y.Comment == null ? "" : y.Comment;

                    return xComment.CompareTo(yComment);
                });
            }
            else if (sortBy == LanguagesManager.Instance.SearchControl_State)
            {
                _searchListViewItemCollection.Sort(delegate(SearchListViewItem x, SearchListViewItem y)
                {
                    return x.State.CompareTo(y.State);
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                _searchListViewItemCollection.Reverse();
            }

            _searchListView.Items.Refresh();
        }

        #endregion

        private class SearchListViewItem
        {
            public string Name { get; set; }
            public string Signature { get; set; }
            public SearchState State { get; set; }
            public IEnumerable<string> Keywords { get; set; }
            public DateTime CreationTime { get; set; }
            public long Length { get; set; }
            public string Comment { get; set; }
            public Seed Value { get; set; }

            public override int GetHashCode()
            {
                if (this.Value == null) return 0;
                else return this.Value.GetHashCode();
            }
        }

        class SeedEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if (object.ReferenceEquals(x, y))
                    return true;
                if (x == null || y == null)
                    return false;

                if (x.Length != y.Length
                    || ((x.Keywords == null) != (y.Keywords == null))
                    //|| x.CreationTime != y.CreationTime
                    || x.Name != y.Name
                    || x.Comment != y.Comment
                    || x.Rank != y.Rank

                    || x.Key != y.Key

                    || x.CompressionAlgorithm != y.CompressionAlgorithm

                    || x.CryptoAlgorithm != y.CryptoAlgorithm
                    || ((x.CryptoKey == null) != (y.CryptoKey == null))

                    || x.Certificate != y.Certificate)
                {
                    return false;
                }

                if (x.Keywords != null && y.Keywords != null)
                {
                    if (!Collection.Equals(x.Keywords, y.Keywords)) return false;
                }

                if (x.CryptoKey != null && y.CryptoKey != null)
                {
                    if (!Collection.Equals(x.CryptoKey, y.CryptoKey)) return false;
                }

                return true;
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                else if (obj.Key == null) return 0;
                else return obj.Key.GetHashCode();
            }
        }

        class SeedHashEqualityComparer : IEqualityComparer<Seed>
        {
            public bool Equals(Seed x, Seed y)
            {
                if (object.ReferenceEquals(x, y))
                    return true;
                if (x == null || y == null)
                    return false;

                if (x.Length != y.Length
                    //|| ((x.Keywords == null) != (y.Keywords == null))
                    //|| x.CreationTime != y.CreationTime
                    //|| x.Name != y.Name
                    //|| x.Comment != y.Comment
                    || x.Rank != y.Rank

                    || x.Key != y.Key

                    || x.CompressionAlgorithm != y.CompressionAlgorithm

                    || x.CryptoAlgorithm != y.CryptoAlgorithm
                    || ((x.CryptoKey == null) != (y.CryptoKey == null)))

                    //|| x.Signature != y.Signature
                    //|| x.DigitalSignatureAlgorithm != y.DigitalSignatureAlgorithm
                    //|| ((x.PublicKey == null) != (y.PublicKey == null))
                    //|| ((x.DigitalSignature == null) != (y.DigitalSignature == null)))
                {
                    return false;
                }

                //if (x.Keywords != null && y.Keywords != null)
                //{
                //    if (x.Keywords.Count != y.Keywords.Count) return false;

                //    for (int i = 0; i < x.Keywords.Count; i++)
                //    {
                //        if (x.Keywords[i] != y.Keywords[i]) return false;
                //    }
                //}

                if (x.CryptoKey != null && y.CryptoKey != null)
                {
                    if (!Collection.Equals(x.CryptoKey, y.CryptoKey)) return false;
                }

                //if (x.PublicKey != null && y.PublicKey != null)
                //{
                //    if (!Collection.Equals(x.PublicKey, y.PublicKey)) return false;
                //}

                //if (x.DigitalSignature != null && y.DigitalSignature != null)
                //{
                //    if (!Collection.Equals(x.DigitalSignature, y.DigitalSignature)) return false;
                //}

                return true;
            }

            public int GetHashCode(Seed obj)
            {
                if (obj == null) return 0;
                else if (obj.Key == null) return 0;
                else return obj.Key.GetHashCode();
            }
        }
    }

    public class SearchTreeViewItem : TreeViewItem
    {
        private int _hit;
        private SearchTreeItem _value;

        public SearchTreeViewItem()
            : base()
        {
            this.Value = new SearchTreeItem()
            {
                SearchItem = new SearchItem()
                {
                    Name = "",
                },
            };

            base.IsExpanded = true;
        }

        public SearchTreeViewItem(SearchTreeItem searchTreeItem)
            : this()
        {
            this.Value = searchTreeItem;

            base.IsExpanded = true;
        }

        public void Update()
        {
            base.Header = new TextBlock()
            {
                Text = string.Format("{0} ({1})", _value.SearchItem.Name, _hit)
            };

            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

            foreach (var item in this.Value.Items)
            {
                list.Add(new SearchTreeViewItem(item));
            }

            foreach (var item in this.Items.OfType<SearchTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    this.Items.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!this.Items.OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    this.Items.Add(item);
                }
            }
        }

        public SearchTreeItem Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }

        public int Hit
        {
            get
            {
                return _hit;
            }
            set
            {
                _hit = value;

                this.Update();
            }
        }
    }

    [DataContract(Name = "SearchTreeItem", Namespace = "http://Amoeba/Windows")]
    public class SearchTreeItem : IDeepCloneable<SearchTreeItem>
    {
        private SearchItem _searchItem;
        private List<SearchTreeItem> _items;

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                return _searchItem;
            }
            set
            {
                _searchItem = value;
            }
        }

        [DataMember(Name = "Items")]
        public List<SearchTreeItem> Items
        {
            get
            {
                if (_items == null)
                    _items = new List<SearchTreeItem>();

                return _items;
            }
        }

        #region IDeepClone<SearchTreeItem> メンバ

        public SearchTreeItem DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchTreeItem));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchTreeItem)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchItem", Namespace = "http://Amoeba/Windows")]
    public class SearchItem : IDeepCloneable<SearchItem>
    {
        private string _name;
        private List<SearchContains<string>> _searchNameCollection;
        private List<SearchContains<string>> _searchNameRegexCollection;
        private List<SearchContains<string>> _searchSignatureCollection;
        private List<SearchContains<string>> _searchKeywordCollection;
        private List<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private List<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private List<SearchContains<Seed>> _searchSeedCollection;

        public SearchItem()
        {

        }

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        [DataMember(Name = "SearchNameCollection")]
        public List<SearchContains<string>> SearchNameCollection
        {
            get
            {
                if (_searchNameCollection == null)
                    _searchNameCollection = new List<SearchContains<string>>();

                return _searchNameCollection;
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public List<SearchContains<string>> SearchNameRegexCollection
        {
            get
            {
                if (_searchNameRegexCollection == null)
                    _searchNameRegexCollection = new List<SearchContains<string>>();

                return _searchNameRegexCollection;
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public List<SearchContains<string>> SearchSignatureCollection
        {
            get
            {
                if (_searchSignatureCollection == null)
                    _searchSignatureCollection = new List<SearchContains<string>>();

                return _searchSignatureCollection;
            }
        }

        [DataMember(Name = "SearchKeywordCollection")]
        public List<SearchContains<string>> SearchKeywordCollection
        {
            get
            {
                if (_searchKeywordCollection == null)
                    _searchKeywordCollection = new List<SearchContains<string>>();

                return _searchKeywordCollection;
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public List<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                if (_searchCreationTimeRangeCollection == null)
                    _searchCreationTimeRangeCollection = new List<SearchContains<SearchRange<DateTime>>>();

                return _searchCreationTimeRangeCollection;
            }
        }

        [DataMember(Name = "SearchLengthRangeCollection")]
        public List<SearchContains<SearchRange<long>>> SearchLengthRangeCollection
        {
            get
            {
                if (_searchLengthRangeCollection == null)
                    _searchLengthRangeCollection = new List<SearchContains<SearchRange<long>>>();

                return _searchLengthRangeCollection;
            }
        }

        [DataMember(Name = "SearchSeedCollection")]
        public List<SearchContains<Seed>> SearchSeedCollection
        {
            get
            {
                if (_searchSeedCollection == null)
                    _searchSeedCollection = new List<SearchContains<Seed>>();

                return _searchSeedCollection;
            }
        }

        public override string ToString()
        {
            return string.Format("Name = {0}", this.Name);
        }

        #region IDeepClone<SearchItem> メンバ

        public SearchItem DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchItem));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchItem)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [Flags]
    [DataContract(Name = "SearchState", Namespace = "http://Amoeba/Windows")]
    public enum SearchState
    {
        [EnumMember(Value = "Searching")]
        Searching = 0x1,

        [EnumMember(Value = "Uploading")]
        Uploading = 0x2,

        [EnumMember(Value = "Uploaded")]
        Uploaded = 0x4,

        [EnumMember(Value = "Downloading")]
        Downloading = 0x8,

        [EnumMember(Value = "Downloaded")]
        Downloaded = 0x10,
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    public class SearchContains<T> : IDeepCloneable<SearchContains<T>>
    {
        [DataMember(Name = "Contains")]
        public bool Contains { get; set; }

        [DataMember(Name = "Value")]
        public T Value { get; set; }

        #region IDeepClone<SearchContains<T>> メンバ

        public SearchContains<T> DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchContains<T>));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchContains<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/Windows")]
    public class SearchRange<T> : IDeepCloneable<SearchRange<T>>
        where T : IComparable
    {
        T _max;
        T _min;

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
            }
        }

        public bool Verify(T value)
        {
            if (value.CompareTo(this.Min) < 0 || value.CompareTo(this.Max) > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override string ToString()
        {
            return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
        }

        #region IDeepClone<SearchRange<T>> メンバ

        public SearchRange<T> DeepClone()
        {
            var ds = new DataContractSerializer(typeof(SearchRange<T>));

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                ms.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchRange<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }
}

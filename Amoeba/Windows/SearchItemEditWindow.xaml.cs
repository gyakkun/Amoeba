using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Library;
using Library.Net.Amoeba;
using Amoeba.Properties;

namespace Amoeba.Windows
{
    /// <summary>
    /// SearchItemEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SearchItemEditWindow : Window
    {
        private SearchItem _searchItem;
        private List<SearchContains<string>> _searchNameCollection;
        private List<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private List<SearchContains<string>> _searchSignatureCollection;
        private List<SearchContains<string>> _searchKeywordCollection;
        private List<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private List<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private List<SearchContains<Seed>> _searchSeedCollection;

        public SearchItemEditWindow(ref SearchItem searchItem)
        {
            _searchItem = searchItem;

            _searchNameCollection = _searchItem.SearchNameCollection.Select(n => n.DeepClone()).ToList();
            _searchNameRegexCollection = _searchItem.SearchNameRegexCollection.Select(n => n.DeepClone()).ToList();
            _searchSignatureCollection = _searchItem.SearchSignatureCollection.Select(n => n.DeepClone()).ToList();
            _searchKeywordCollection = _searchItem.SearchKeywordCollection.Select(n => n.DeepClone()).ToList();
            _searchCreationTimeRangeCollection = _searchItem.SearchCreationTimeRangeCollection.Select(n => n.DeepClone()).ToList();
            _searchLengthRangeCollection = _searchItem.SearchLengthRangeCollection.Select(n => n.DeepClone()).ToList();
            _searchSeedCollection = _searchItem.SearchSeedCollection.Select(n => n.DeepClone()).ToList();

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameContainsCheckBox.IsChecked = true;
            _nameRegexContainsCheckBox.IsChecked = true;
            _signatureContainsCheckBox.IsChecked = true;
            _keywordContainsCheckBox.IsChecked = true;
            _creationTimeRangeContainsCheckBox.IsChecked = true;
            _lengthRangeContainsCheckBox.IsChecked = true;
            _seedContainsCheckBox.IsChecked = true;

            _nameListView.ItemsSource = _searchNameCollection;
            _nameRegexListView.ItemsSource = _searchNameRegexCollection;
            _signatureListView.ItemsSource = _searchSignatureCollection;
            _keywordListView.ItemsSource = _searchKeywordCollection;
            _creationTimeRangeListView.ItemsSource = _searchCreationTimeRangeCollection;
            _lengthRangeListView.ItemsSource = _searchLengthRangeCollection;
            _seedListView.ItemsSource = _searchSeedCollection;

            _searchTreeViewItemNameTextBox.Text = _searchItem.Name;

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);

            if ((_searchItem.SearchState & SearchState.Cache) == SearchState.Cache)
            {
                _miscellaneousSearchFilterCacheCheckBox.IsChecked = true;
            }
            if ((_searchItem.SearchState & SearchState.Uploading) == SearchState.Uploading)
            {
                _miscellaneousSearchFilterUploadingCheckBox.IsChecked = true;
            }
            if ((_searchItem.SearchState & SearchState.Downloading) == SearchState.Downloading)
            {
                _miscellaneousSearchFilterDownloadingCheckBox.IsChecked = true;
            }
            if ((_searchItem.SearchState & SearchState.Uploaded) == SearchState.Uploaded)
            {
                _miscellaneousSearchFilterUploadedCheckBox.IsChecked = true;
            }
            if ((_searchItem.SearchState & SearchState.Downloaded) == SearchState.Downloaded)
            {
                _miscellaneousSearchFilterDownloadedCheckBox.IsChecked = true;
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            _searchItem.Name = _searchTreeViewItemNameTextBox.Text;

            _searchItem.SearchNameCollection.Clear();
            _searchItem.SearchNameCollection.AddRange(_searchNameCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchNameRegexCollection.Clear();
            _searchItem.SearchNameRegexCollection.AddRange(_searchNameRegexCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchSignatureCollection.Clear();
            _searchItem.SearchSignatureCollection.AddRange(_searchSignatureCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchKeywordCollection.Clear();
            _searchItem.SearchKeywordCollection.AddRange(_searchKeywordCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchCreationTimeRangeCollection.Clear();
            _searchItem.SearchCreationTimeRangeCollection.AddRange(_searchCreationTimeRangeCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchLengthRangeCollection.Clear();
            _searchItem.SearchLengthRangeCollection.AddRange(_searchLengthRangeCollection.Select(n => n.DeepClone()).ToList());
            _searchItem.SearchSeedCollection.Clear();
            _searchItem.SearchSeedCollection.AddRange(_searchSeedCollection.Select(n => n.DeepClone()).ToList());

            SearchState state = 0;

            if (_miscellaneousSearchFilterCacheCheckBox.IsChecked.Value)
            {
                state |= SearchState.Cache;
            }
            if (_miscellaneousSearchFilterUploadingCheckBox.IsChecked.Value)
            {
                state |= SearchState.Uploading;
            }
            if (_miscellaneousSearchFilterDownloadingCheckBox.IsChecked.Value)
            {
                state |= SearchState.Downloading;
            }
            if (_miscellaneousSearchFilterUploadedCheckBox.IsChecked.Value)
            {
                state |= SearchState.Uploaded;
            }
            if (_miscellaneousSearchFilterDownloadedCheckBox.IsChecked.Value)
            {
                state |= SearchState.Downloaded;
            }

            _searchItem.SearchState = state;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        #region _nameListView

        private void _nameListViewUpdate()
        {
            _nameListView_SelectionChanged(this, null);
        }

        private void _nameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _nameListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _nameUpButton.IsEnabled = false;
                    _nameDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _nameUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _nameUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchNameCollection.Count - 1)
                    {
                        _nameDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _nameDownButton.IsEnabled = true;
                    }
                }

                _nameListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _nameListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _nameContainsCheckBox.IsChecked = true;
                _nameTextBox.Text = "";
                return;
            }

            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _nameContainsCheckBox.IsChecked = item.Contains;
            _nameTextBox.Text = item.Value;
        }

        private void _nameListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _nameListView.SelectedItems;

            _nameListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _nameListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
        }

        private void _nameListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _nameListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _nameListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
                    };

                    if (_searchNameCollection.Contains(item)) continue;
                    _searchNameCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _nameTextBox.Text = "";
            _nameListView.SelectedIndex = _searchNameCollection.Count - 1;

            _nameListView.Items.Refresh();
            _nameListViewUpdate();
        }

        private void _nameUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameCollection.Remove(item);
            _searchNameCollection.Insert(selectIndex - 1, item);
            _nameListView.Items.Refresh();

            _nameListViewUpdate();
        }

        private void _nameDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameCollection.Remove(item);
            _searchNameCollection.Insert(selectIndex + 1, item);
            _nameListView.Items.Refresh();

            _nameListViewUpdate();
        }

        private void _nameAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameTextBox.Text == "") return;

            var item = new SearchContains<string>()
            {
                Contains = _nameContainsCheckBox.IsChecked.Value,
                Value = _nameTextBox.Text,
            };

            if (_searchNameCollection.Contains(item)) return;
            _searchNameCollection.Add(item);

            _nameTextBox.Text = "";
            _nameListView.SelectedIndex = _searchNameCollection.Count - 1;

            _nameListView.Items.Refresh();
            _nameListViewUpdate();
        }

        private void _nameEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameTextBox.Text == "") return;

            var uitem = new SearchContains<string>()
            {
                Contains = _nameContainsCheckBox.IsChecked.Value,
                Value = _nameTextBox.Text,
            };

            if (_searchNameCollection.Contains(uitem)) return;

            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _nameContainsCheckBox.IsChecked.Value;
            item.Value = _nameTextBox.Text;

            _nameListView.Items.Refresh();
            _nameListViewUpdate();
        }

        private void _nameDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _nameTextBox.Text = "";

            int selectIndex = _nameListView.SelectedIndex;
            _searchNameCollection.Remove(item);
            _nameListView.Items.Refresh();
            _nameListView.SelectedIndex = selectIndex;
            _nameListViewUpdate();
        }

        #endregion

        #region _nameRegexListView

        private void _nameRegexListViewUpdate()
        {
            _nameRegexListView_SelectionChanged(this, null);
        }

        private void _nameRegexListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _nameRegexListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _nameRegexUpButton.IsEnabled = false;
                    _nameRegexDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _nameRegexUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _nameRegexUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchNameRegexCollection.Count - 1)
                    {
                        _nameRegexDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _nameRegexDownButton.IsEnabled = true;
                    }
                }

                _nameRegexListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _nameRegexListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _nameRegexContainsCheckBox.IsChecked = true;
                _nameRegexIsIgnoreCaseCheckBox.IsChecked = false;
                _nameRegexTextBox.Text = "";
                return;
            }

            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _nameRegexContainsCheckBox.IsChecked = item.Contains;
            _nameRegexIsIgnoreCaseCheckBox.IsChecked = item.Value.IsIgnoreCase;
            _nameRegexTextBox.Text = item.Value.Value;
        }

        private void _nameRegexListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _nameRegexListView.SelectedItems;

            _nameRegexListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _nameRegexListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) ([\+-]) (.*)"));
            }
        }

        private void _nameRegexListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _nameRegexListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} {2}", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _nameRegexListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) ([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    try
                    {
                        new Regex(match.Groups[3].Value);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    var item = new SearchContains<SearchRegex>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRegex()
                        {
                            IsIgnoreCase = (match.Groups[2].Value == "+") ? true : false,
                            Value = match.Groups[3].Value
                        },
                    };

                    if (_searchNameRegexCollection.Contains(item)) continue;
                    _searchNameRegexCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _nameRegexTextBox.Text = "";
            _nameRegexListView.SelectedIndex = _searchNameRegexCollection.Count - 1;

            _nameRegexListView.Items.Refresh();
            _nameRegexListViewUpdate();
        }

        private void _nameRegexUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameRegexCollection.Remove(item);
            _searchNameRegexCollection.Insert(selectIndex - 1, item);
            _nameRegexListView.Items.Refresh();

            _nameRegexListViewUpdate();
        }

        private void _nameRegexDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameRegexCollection.Remove(item);
            _searchNameRegexCollection.Insert(selectIndex + 1, item);
            _nameRegexListView.Items.Refresh();

            _nameRegexListViewUpdate();
        }

        private void _nameRegexAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameRegexTextBox.Text == "") return;

            try
            {
                new Regex(_nameRegexTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            var item = new SearchContains<SearchRegex>()
            {
                Contains = _nameRegexContainsCheckBox.IsChecked.Value,
                Value = new SearchRegex()
                {
                    IsIgnoreCase = _nameRegexIsIgnoreCaseCheckBox.IsChecked.Value,
                    Value = _nameRegexTextBox.Text
                },
            };

            if (_searchNameRegexCollection.Contains(item)) return;
            _searchNameRegexCollection.Add(item);

            _nameRegexTextBox.Text = "";
            _nameRegexListView.SelectedIndex = _searchNameRegexCollection.Count - 1;

            _nameRegexListView.Items.Refresh();
            _nameRegexListViewUpdate();
        }

        private void _nameRegexEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameRegexTextBox.Text == "") return;

            try
            {
                new Regex(_nameRegexTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            var uitem = new SearchContains<SearchRegex>()
            {
                Contains = _nameRegexContainsCheckBox.IsChecked.Value,
                Value = new SearchRegex()
                {
                    IsIgnoreCase = _nameRegexIsIgnoreCaseCheckBox.IsChecked.Value,
                    Value = _nameRegexTextBox.Text
                },
            };

            if (_searchNameRegexCollection.Contains(uitem)) return;

            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            item.Contains = _nameRegexContainsCheckBox.IsChecked.Value;
            item.Value = new SearchRegex() { IsIgnoreCase = _nameRegexIsIgnoreCaseCheckBox.IsChecked.Value, Value = _nameRegexTextBox.Text };

            _nameRegexListView.Items.Refresh();
            _nameRegexListViewUpdate();
        }

        private void _nameRegexDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _nameRegexTextBox.Text = "";

            int selectIndex = _nameRegexListView.SelectedIndex;
            _searchNameRegexCollection.Remove(item);
            _nameRegexListView.Items.Refresh();
            _nameRegexListView.SelectedIndex = selectIndex;
            _nameRegexListViewUpdate();
        }

        #endregion

        #region _signatureListView

        private void _signatureListViewUpdate()
        {
            _signatureListView_SelectionChanged(this, null);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _signatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _signatureUpButton.IsEnabled = false;
                    _signatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _signatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchSignatureCollection.Count - 1)
                    {
                        _signatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureDownButton.IsEnabled = true;
                    }
                }

                _signatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _signatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _signatureContainsCheckBox.IsChecked = true;
                _signatureTextBox.Text = "";
                return;
            }

            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _signatureContainsCheckBox.IsChecked = item.Contains;
            _signatureTextBox.Text = item.Value;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _signatureListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
        }

        private void _signatureListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    if (!Regex.IsMatch(match.Groups[2].Value, @"^[a-zA-Z0-9\-_]*$")) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
                    };

                    if (_searchSignatureCollection.Contains(item)) continue;
                    _searchSignatureCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _searchSignatureCollection.Count - 1;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSignatureCollection.Remove(item);
            _searchSignatureCollection.Insert(selectIndex - 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSignatureCollection.Remove(item);
            _searchSignatureCollection.Insert(selectIndex + 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;
            if (!Regex.IsMatch(_signatureTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            var item = new SearchContains<string>()
            {
                Contains = _signatureContainsCheckBox.IsChecked.Value,
                Value = _signatureTextBox.Text,
            };

            if (_searchSignatureCollection.Contains(item)) return;
            _searchSignatureCollection.Add(item);

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _searchSignatureCollection.Count - 1;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;
            if (!Regex.IsMatch(_signatureTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            var uitem = new SearchContains<string>()
            {
                Contains = _signatureContainsCheckBox.IsChecked.Value,
                Value = _signatureTextBox.Text,
            };

            if (_searchSignatureCollection.Contains(uitem)) return;

            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _signatureContainsCheckBox.IsChecked.Value;
            item.Value = _signatureTextBox.Text;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _signatureTextBox.Text = "";

            int selectIndex = _signatureListView.SelectedIndex;
            _searchSignatureCollection.Remove(item);
            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
            _signatureListViewUpdate();
        }

        #endregion

        #region _keywordListView

        private void _keywordListViewUpdate()
        {
            _keywordListView_SelectionChanged(this, null);
        }

        private void _keywordListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _keywordListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _keywordUpButton.IsEnabled = false;
                    _keywordDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _keywordUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _keywordUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchKeywordCollection.Count - 1)
                    {
                        _keywordDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _keywordDownButton.IsEnabled = true;
                    }
                }

                _keywordListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _keywordListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _keywordContainsCheckBox.IsChecked = true;
                _keywordTextBox.Text = "";
                return;
            }

            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _keywordContainsCheckBox.IsChecked = item.Contains;
            _keywordTextBox.Text = item.Value;
        }

        private void _keywordListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _keywordListView.SelectedItems;

            _keywordListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _keywordListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
        }

        private void _keywordListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _keywordListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _keywordListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    if (!Regex.IsMatch(match.Groups[2].Value, "^[a-z0-9_]*$")) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
                    };

                    if (_searchKeywordCollection.Contains(item)) continue;
                    _searchKeywordCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _keywordTextBox.Text = "";
            _keywordListView.SelectedIndex = _searchKeywordCollection.Count - 1;

            _keywordListView.Items.Refresh();
            _keywordListViewUpdate();
        }

        private void _keywordUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchKeywordCollection.Remove(item);
            _searchKeywordCollection.Insert(selectIndex - 1, item);
            _keywordListView.Items.Refresh();

            _keywordListViewUpdate();
        }

        private void _keywordDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchKeywordCollection.Remove(item);
            _searchKeywordCollection.Insert(selectIndex + 1, item);
            _keywordListView.Items.Refresh();

            _keywordListViewUpdate();
        }

        private void _keywordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;
            if (!Regex.IsMatch(_keywordTextBox.Text, "^[a-z0-9_]*$")) return;

            var item = new SearchContains<string>()
            {
                Contains = _keywordContainsCheckBox.IsChecked.Value,
                Value = _keywordTextBox.Text,
            };

            if (_searchKeywordCollection.Contains(item)) return;
            _searchKeywordCollection.Add(item);

            _keywordTextBox.Text = "";
            _keywordListView.SelectedIndex = _searchKeywordCollection.Count - 1;

            _keywordListView.Items.Refresh();
            _keywordListViewUpdate();
        }

        private void _keywordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;
            if (!Regex.IsMatch(_keywordTextBox.Text, "^[a-z0-9_]*$")) return;

            var uitem = new SearchContains<string>()
            {
                Contains = _keywordContainsCheckBox.IsChecked.Value,
                Value = _keywordTextBox.Text,
            };

            if (_searchKeywordCollection.Contains(uitem)) return;

            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _keywordContainsCheckBox.IsChecked.Value;
            item.Value = _keywordTextBox.Text;

            _keywordListView.Items.Refresh();
            _keywordListViewUpdate();
        }

        private void _keywordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _keywordTextBox.Text = "";

            int selectIndex = _keywordListView.SelectedIndex;
            _searchKeywordCollection.Remove(item);
            _keywordListView.Items.Refresh();
            _keywordListView.SelectedIndex = selectIndex;
            _keywordListViewUpdate();
        }

        #endregion

        #region _creationTimeRangeListView

        private void _creationTimeRangeListViewUpdate()
        {
            _creationTimeRangeListView_SelectionChanged(this, null);
        }

        private void _creationTimeRangeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _creationTimeRangeListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _creationTimeRangeUpButton.IsEnabled = false;
                    _creationTimeRangeDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _creationTimeRangeUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _creationTimeRangeUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchCreationTimeRangeCollection.Count - 1)
                    {
                        _creationTimeRangeDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _creationTimeRangeDownButton.IsEnabled = true;
                    }
                }

                _creationTimeRangeListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _creationTimeRangeListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _creationTimeRangeContainsCheckBox.IsChecked = true; ;
                _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _creationTimeRangeMaxTextBox.Text = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                return;
            }

            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            _creationTimeRangeContainsCheckBox.IsChecked = item.Contains;
            _creationTimeRangeMinTextBox.Text = item.Value.Min.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = item.Value.Max.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        private void _creationTimeRangeListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _creationTimeRangeListView.SelectedItems;

            _creationTimeRangeListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _creationTimeRangeListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*), (.*)"));
            }
        }

        private void _creationTimeRangeListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _creationTimeRangeListView.SelectedItems.OfType<SearchContains<SearchRange<DateTime>>>())
            {
                sb.AppendLine(string.Format("{0} {1}, {2}", (item.Contains == true) ? "+" : "-",
                    item.Value.Min.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss"),
                    item.Value.Max.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss")));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _creationTimeRangeListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*), (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchRange<DateTime>>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRange<DateTime>()
                        {
                            Max = DateTime.ParseExact(match.Groups[3].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                            Min = DateTime.ParseExact(match.Groups[2].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                        },
                    };

                    if (_searchCreationTimeRangeCollection.Contains(item)) continue;
                    _searchCreationTimeRangeCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeListView.SelectedIndex = _searchCreationTimeRangeCollection.Count - 1;

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchCreationTimeRangeCollection.Remove(item);
            _searchCreationTimeRangeCollection.Insert(selectIndex - 1, item);
            _creationTimeRangeListView.Items.Refresh();

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchCreationTimeRangeCollection.Remove(item);
            _searchCreationTimeRangeCollection.Insert(selectIndex + 1, item);
            _creationTimeRangeListView.Items.Refresh();

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<DateTime>()
                    {
                        Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                        Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                    }
                };

                if (_searchCreationTimeRangeCollection.Contains(item)) return;
                _searchCreationTimeRangeCollection.Add(item);

                _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _creationTimeRangeMaxTextBox.Text = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _creationTimeRangeListView.SelectedIndex = _searchCreationTimeRangeCollection.Count - 1;
            }
            catch (Exception)
            {
                return;
            }

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            try
            {
                var uitem = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<DateTime>()
                    {
                        Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                        Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                    }
                };

                if (_searchCreationTimeRangeCollection.Contains(uitem)) return;

                var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
                if (item == null) return;

                item.Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value;
                item.Value.Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
                item.Value.Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }
            catch (Exception)
            {
                return;
            }

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc).ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);

            int selectIndex = _creationTimeRangeListView.SelectedIndex;
            _searchCreationTimeRangeCollection.Remove(item);
            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListView.SelectedIndex = selectIndex;
            _creationTimeRangeListViewUpdate();
        }

        #endregion

        #region _lengthRangeListView

        private void _lengthRangeListViewUpdate()
        {
            _lengthRangeListView_SelectionChanged(this, null);
        }

        private void _lengthRangeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _lengthRangeListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _lengthRangeUpButton.IsEnabled = false;
                    _lengthRangeDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _lengthRangeUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _lengthRangeUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchLengthRangeCollection.Count - 1)
                    {
                        _lengthRangeDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _lengthRangeDownButton.IsEnabled = true;
                    }
                }

                _lengthRangeListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _lengthRangeListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _lengthRangeContainsCheckBox.IsChecked = true;
                _lengthRangeMinTextBox.Text = "";
                _lengthRangeMaxTextBox.Text = "";
                return;
            }

            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            _lengthRangeContainsCheckBox.IsChecked = item.Contains;
            _lengthRangeMinTextBox.Text = item.Value.Min.ToString();
            _lengthRangeMaxTextBox.Text = item.Value.Max.ToString();
        }

        private void _lengthRangeListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _lengthRangeListView.SelectedItems;

            _lengthRangeListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _lengthRangeListViewPasteContextMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*), (.*)"));
            }
        }

        private void _lengthRangeListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _lengthRangeListView.SelectedItems.OfType<SearchContains<SearchRange<long>>>())
            {
                sb.AppendLine(string.Format("{0} {1}, {2}", (item.Contains == true) ? "+" : "-", item.Value.Min, item.Value.Max));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _lengthRangeListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*), (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchRange<long>>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRange<long>()
                        {
                            Max = long.Parse(match.Groups[3].Value),
                            Min = long.Parse(match.Groups[2].Value),
                        },
                    };

                    if (_searchLengthRangeCollection.Contains(item)) continue;
                    _searchLengthRangeCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _lengthRangeMinTextBox.Text = "";
            _lengthRangeMaxTextBox.Text = "";
            _lengthRangeListView.SelectedIndex = _searchLengthRangeCollection.Count - 1;

            _lengthRangeListView.Items.Refresh();
            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            var selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchLengthRangeCollection.Remove(item);
            _searchLengthRangeCollection.Insert(selectIndex - 1, item);
            _lengthRangeListView.Items.Refresh();

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            var selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchLengthRangeCollection.Remove(item);
            _searchLengthRangeCollection.Insert(selectIndex + 1, item);
            _lengthRangeListView.Items.Refresh();

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lengthRangeMinTextBox.Text == "") return;
            if (_lengthRangeMaxTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRange<long>>()
                {
                    Contains = _lengthRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<long>()
                    {
                        Max = long.Parse(_lengthRangeMaxTextBox.Text),
                        Min = long.Parse(_lengthRangeMinTextBox.Text),
                    }
                };

                if (_searchLengthRangeCollection.Contains(item)) return;
                _searchLengthRangeCollection.Add(item);

                _lengthRangeMinTextBox.Text = "";
                _lengthRangeMaxTextBox.Text = "";
                _lengthRangeListView.SelectedIndex = _searchLengthRangeCollection.Count - 1;
            }
            catch (Exception)
            {
                return;
            }

            _lengthRangeListView.Items.Refresh();
            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lengthRangeMinTextBox.Text == "") return;
            if (_lengthRangeMaxTextBox.Text == "") return;

            try
            {
                var uitem = new SearchContains<SearchRange<long>>()
                {
                    Contains = _lengthRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<long>()
                    {
                        Max = long.Parse(_lengthRangeMaxTextBox.Text),
                        Min = long.Parse(_lengthRangeMinTextBox.Text),
                    }
                };

                if (_searchLengthRangeCollection.Contains(uitem)) return;

                var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
                if (item == null) return;

                item.Contains = _lengthRangeContainsCheckBox.IsChecked.Value;
                item.Value.Max = long.Parse(_lengthRangeMaxTextBox.Text);
                item.Value.Min = long.Parse(_lengthRangeMinTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            _lengthRangeListView.Items.Refresh();
            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            _lengthRangeMinTextBox.Text = "";
            _lengthRangeMaxTextBox.Text = "";

            int selectIndex = _lengthRangeListView.SelectedIndex;
            _searchLengthRangeCollection.Remove(item);
            _lengthRangeListView.Items.Refresh();
            _lengthRangeListView.SelectedIndex = selectIndex;
            _lengthRangeListViewUpdate();
        }

        #endregion

        #region _seedListView

        private void _seedListViewUpdate()
        {
            _seedListView_SelectionChanged(this, null);
        }

        private void _seedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _seedListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _seedUpButton.IsEnabled = false;
                    _seedDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _seedUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _seedUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchSeedCollection.Count - 1)
                    {
                        _seedDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _seedDownButton.IsEnabled = true;
                    }
                }

                _seedListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _seedListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _seedContainsCheckBox.IsChecked = true;
                _seedTextBox.Text = "";
                _seedNameTextBox.Text = "";
                return;
            }

            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            _seedContainsCheckBox.IsChecked = item.Contains;
            _seedTextBox.Text = AmoebaConverter.ToSeedString(item.Value);
            _seedNameTextBox.Text = string.Format("{0}, {1:#,0}", item.Value.Name, item.Value.Length);
        }

        private void _seedListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _seedListView.SelectedItems;

            _seedListViewCopyContextMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var seeds = Clipboard.GetSeeds();
                var text = Clipboard.GetText();

                _seedListViewPasteContextMenuItem.IsEnabled = ((seeds.Count() > 0) || (text != null && Regex.IsMatch(text, @"([\+-]) (.*)")));
            }
        }

        private void _seedListViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _seedListView.SelectedItems.OfType<SearchContains<Seed>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", AmoebaConverter.ToSeedString(item.Value)));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _seedListViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var seed in Clipboard.GetSeeds())
            {
                try
                {
                    var item = new SearchContains<Seed>()
                    {
                        Contains = false,
                        Value = seed,
                    };

                    if (_searchSeedCollection.Contains(item)) continue;
                    _searchSeedCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<Seed>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = AmoebaConverter.FromSeedString(match.Groups[2].Value),
                    };

                    if (_searchSeedCollection.Contains(item)) continue;
                    _searchSeedCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _seedTextBox.Text = "";
            _seedListView.SelectedIndex = _searchSeedCollection.Count - 1;

            _seedListView.Items.Refresh();
            _seedListViewUpdate();
        }

        private void _seedUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            var selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSeedCollection.Remove(item);
            _searchSeedCollection.Insert(selectIndex - 1, item);
            _seedListView.Items.Refresh();

            _seedListViewUpdate();
        }

        private void _seedDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            var selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSeedCollection.Remove(item);
            _searchSeedCollection.Insert(selectIndex + 1, item);
            _seedListView.Items.Refresh();

            _seedListViewUpdate();
        }

        private void _seedAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_seedTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<Seed>()
                {
                    Contains = _seedContainsCheckBox.IsChecked.Value,
                    Value = AmoebaConverter.FromSeedString(_seedTextBox.Text),
                };

                if (_searchSeedCollection.Contains(item)) return;
                _searchSeedCollection.Add(item);

                _seedTextBox.Text = "";
                _seedListView.SelectedIndex = _searchSeedCollection.Count - 1;
            }
            catch (Exception)
            {
                return;
            }

            _seedListView.Items.Refresh();
            _seedListViewUpdate();
        }

        private void _seedEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_seedTextBox.Text == "") return;

            try
            {
                var uitem = new SearchContains<Seed>()
                {
                    Contains = _seedContainsCheckBox.IsChecked.Value,
                    Value = AmoebaConverter.FromSeedString(_seedTextBox.Text),
                };

                if (_searchSeedCollection.Contains(uitem)) return;

                var item = _seedListView.SelectedItem as SearchContains<Seed>;
                if (item == null) return;

                item.Contains = _seedContainsCheckBox.IsChecked.Value;
                item.Value = AmoebaConverter.FromSeedString(_seedTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            _seedListView.Items.Refresh();
            _seedListViewUpdate();
        }

        private void _seedDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            _seedTextBox.Text = "";

            int selectIndex = _seedListView.SelectedIndex;
            _searchSeedCollection.Remove(item);
            _seedListView.Items.Refresh();
            _seedListView.SelectedIndex = selectIndex;
            _seedListViewUpdate();
        }

        #endregion
    }
}

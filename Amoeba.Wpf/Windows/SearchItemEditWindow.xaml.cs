using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Amoeba.Properties;
using Library;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    /// <summary>
    /// SearchItemEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SearchItemEditWindow : Window
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        private SearchItem _searchItem;

        private ObservableCollectionEx<SearchContains<string>> _nameCollection;
        private ObservableCollectionEx<SearchContains<SearchRegex>> _nameRegexCollection;
        private ObservableCollectionEx<SearchContains<SearchRegex>> _signatureCollection;
        private ObservableCollectionEx<SearchContains<string>> _keywordCollection;
        private ObservableCollectionEx<SearchContains<SearchRange<DateTime>>> _creationTimeRangeCollection;
        private ObservableCollectionEx<SearchContains<SearchRange<long>>> _lengthRangeCollection;
        private ObservableCollectionEx<SearchContains<Seed>> _seedCollection;
        private ObservableCollectionEx<SearchContains<SearchState>> _stateCollection;

        public SearchItemEditWindow(SearchItem searchItem)
        {
            _searchItem = searchItem;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_searchItem.ThisLock)
            {
                _searchTreeViewItemNameTextBox.Text = _searchItem.Name;

                _nameCollection = new ObservableCollectionEx<SearchContains<string>>(_searchItem.SearchNameCollection);
                _nameRegexCollection = new ObservableCollectionEx<SearchContains<SearchRegex>>(_searchItem.SearchNameRegexCollection);
                _signatureCollection = new ObservableCollectionEx<SearchContains<SearchRegex>>(_searchItem.SearchSignatureCollection);
                _keywordCollection = new ObservableCollectionEx<SearchContains<string>>(_searchItem.SearchKeywordCollection);
                _creationTimeRangeCollection = new ObservableCollectionEx<SearchContains<SearchRange<DateTime>>>(_searchItem.SearchCreationTimeRangeCollection);
                _lengthRangeCollection = new ObservableCollectionEx<SearchContains<SearchRange<long>>>(_searchItem.SearchLengthRangeCollection);
                _seedCollection = new ObservableCollectionEx<SearchContains<Seed>>(_searchItem.SearchSeedCollection);
                _stateCollection = new ObservableCollectionEx<SearchContains<SearchState>>(_searchItem.SearchStateCollection);
            }

            _searchTreeViewItemNameTextBox_TextChanged(null, null);

            _nameListView.ItemsSource = _nameCollection;
            _nameRegexListView.ItemsSource = _nameRegexCollection;
            _signatureListView.ItemsSource = _signatureCollection;
            _keywordListView.ItemsSource = _keywordCollection;
            _creationTimeRangeListView.ItemsSource = _creationTimeRangeCollection;
            _lengthRangeListView.ItemsSource = _lengthRangeCollection;
            _seedListView.ItemsSource = _seedCollection;
            _stateListView.ItemsSource = _stateCollection;

            _nameListViewUpdate();
            _nameRegexListViewUpdate();
            _signatureListViewUpdate();
            _keywordListViewUpdate();
            _creationTimeRangeListViewUpdate();
            _lengthRangeListViewUpdate();
            _seedListViewUpdate();
            _stateListViewUpdate();

            foreach (var item in Enum.GetValues(typeof(SearchState)).Cast<SearchState>())
            {
                _stateComboBox.Items.Add(item);
            }

            _stateComboBox.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void _searchTreeViewItemNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_searchTreeViewItemNameTextBox.Text);
        }

        #region _nameListView

        private void _nameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_nameListView.SelectedIndex == -1)
                {
                    _nameAddButton_Click(null, null);
                }
                else
                {
                    _nameEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _nameCollection.Count - 1)
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

            _nameListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _nameListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _nameListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], "^([\\+-]) \"(.*)\"$");
                }

                _nameListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _nameListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _nameDeleteButton_Click(null, null);
        }

        private void _nameListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _nameListViewCopyMenuItem_Click(null, null);
            _nameDeleteButton_Click(null, null);
        }

        private void _nameListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _nameListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} \"{1}\"", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _nameListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex("^([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<string>(
                       (match.Groups[1].Value == "+") ? true : false,
                        match.Groups[2].Value
                    );

                    if (_nameCollection.Contains(item)) continue;
                    _nameCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _nameListViewUpdate();
        }

        private void _nameUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            _nameCollection.Move(selectIndex, selectIndex - 1);

            _nameListViewUpdate();
        }

        private void _nameDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            _nameCollection.Move(selectIndex, selectIndex + 1);

            _nameListViewUpdate();
        }

        private void _nameAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameTextBox.Text == "") return;

            var item = new SearchContains<string>(
               _nameContainsCheckBox.IsChecked.Value,
               _nameTextBox.Text
            );

            if (_nameCollection.Contains(item)) return;
            _nameCollection.Add(item);

            _nameListViewUpdate();
        }

        private void _nameEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameTextBox.Text == "") return;

            var selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            var item = new SearchContains<string>(
                _nameContainsCheckBox.IsChecked.Value,
                _nameTextBox.Text
            );

            if (_nameCollection.Contains(item)) return;
            _nameCollection.Set(selectIndex, item);

            _nameListView.SelectedIndex = selectIndex;

            _nameListViewUpdate();
        }

        private void _nameDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _nameListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _nameListView.SelectedItems.OfType<SearchContains<string>>().ToArray())
            {
                _nameCollection.Remove(item);
            }

            _nameListViewUpdate();
        }

        #endregion

        #region _nameRegexListView

        private void _nameRegexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_nameRegexListView.SelectedIndex == -1)
                {
                    _nameRegexAddButton_Click(null, null);
                }
                else
                {
                    _nameRegexEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _nameRegexCollection.Count - 1)
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

            _nameRegexListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _nameRegexListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _nameRegexListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], "^([\\+-]) ([\\+-]) \"(.*)\"$");
                }

                _nameRegexListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _nameRegexListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _nameRegexDeleteButton_Click(null, null);
        }

        private void _nameRegexListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _nameRegexListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} \"{2}\"", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _nameRegexListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _nameRegexListViewCopyMenuItem_Click(null, null);
            _nameRegexDeleteButton_Click(null, null);
        }

        private void _nameRegexListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchRegex>(
                        (match.Groups[1].Value == "+") ? true : false,
                        new SearchRegex(match.Groups[3].Value, (match.Groups[2].Value == "+") ? true : false)
                    );

                    if (_nameRegexCollection.Contains(item)) continue;
                    _nameRegexCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _nameRegexListViewUpdate();
        }

        private void _nameRegexUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _nameRegexCollection.Move(selectIndex, selectIndex - 1);

            _nameRegexListViewUpdate();
        }

        private void _nameRegexDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _nameRegexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _nameRegexCollection.Move(selectIndex, selectIndex + 1);

            _nameRegexListViewUpdate();
        }

        private void _nameRegexAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameRegexTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRegex>(
                    _nameRegexContainsCheckBox.IsChecked.Value,
                    new SearchRegex(_nameRegexTextBox.Text, _nameRegexIsIgnoreCaseCheckBox.IsChecked.Value)
                );

                if (_nameRegexCollection.Contains(item)) return;
                _nameRegexCollection.Add(item);
            }
            catch (Exception)
            {

            }

            _nameRegexListViewUpdate();
        }

        private void _nameRegexEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_nameRegexTextBox.Text == "") return;

            int selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var item = new SearchContains<SearchRegex>(
                    _nameRegexContainsCheckBox.IsChecked.Value,
                    new SearchRegex(_nameRegexTextBox.Text, _nameRegexIsIgnoreCaseCheckBox.IsChecked.Value)
                );

                if (_nameRegexCollection.Contains(item)) return;
                _nameRegexCollection.Set(selectIndex, item);

                _nameRegexListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {

            }

            _nameRegexListViewUpdate();
        }

        private void _nameRegexDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _nameRegexListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _nameRegexListView.SelectedItems.OfType<SearchContains<SearchRegex>>().ToArray())
            {
                _nameRegexCollection.Remove(item);
            }

            _nameRegexListViewUpdate();
        }

        #endregion

        #region _signatureListView

        private void _signatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_signatureListView.SelectedIndex == -1)
                {
                    _signatureAddButton_Click(null, null);
                }
                else
                {
                    _signatureEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _signatureCollection.Count - 1)
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
                _signatureIsIgnoreCaseCheckBox.IsChecked = false;
                _signatureTextBox.Text = "";

                return;
            }

            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _signatureContainsCheckBox.IsChecked = item.Contains;
            _signatureIsIgnoreCaseCheckBox.IsChecked = item.Value.IsIgnoreCase;
            _signatureTextBox.Text = item.Value.Value;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], "^([\\+-]) ([\\+-]) \"(.*)\"$");
                }

                _signatureListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} \"{2}\"", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchRegex>(
                        (match.Groups[1].Value == "+") ? true : false,
                        new SearchRegex(match.Groups[3].Value, (match.Groups[2].Value == "+") ? true : false)
                    );

                    if (_signatureCollection.Contains(item)) continue;
                    _signatureCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _signatureListViewUpdate();
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureCollection.Move(selectIndex, selectIndex - 1);

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureCollection.Move(selectIndex, selectIndex + 1);

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRegex>(
                    _signatureContainsCheckBox.IsChecked.Value,
                    new SearchRegex(_signatureTextBox.Text, _signatureIsIgnoreCaseCheckBox.IsChecked.Value)
                );

                if (_signatureCollection.Contains(item)) return;
                _signatureCollection.Add(item);
            }
            catch (Exception)
            {

            }

            _signatureListViewUpdate();
        }

        private void _signatureEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var item = new SearchContains<SearchRegex>(
                    _signatureContainsCheckBox.IsChecked.Value,
                    new SearchRegex(_signatureTextBox.Text, _signatureIsIgnoreCaseCheckBox.IsChecked.Value)
                );

                if (_signatureCollection.Contains(item)) return;
                _signatureCollection.Set(selectIndex, item);

                _signatureListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {

            }

            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<SearchRegex>>().ToArray())
            {
                _signatureCollection.Remove(item);
            }

            _signatureListViewUpdate();
        }

        #endregion

        #region _keywordListView

        private void _keywordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_keywordListView.SelectedIndex == -1)
                {
                    _keywordAddButton_Click(null, null);
                }
                else
                {
                    _keywordEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _keywordCollection.Count - 1)
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

            _keywordListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _keywordListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _keywordListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], "^([\\+-]) \"(.*)\"$");
                }

                _keywordListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _keywordListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _keywordDeleteButton_Click(null, null);
        }

        private void _keywordListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _keywordListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} \"{1}\"", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _keywordListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _keywordListViewCopyMenuItem_Click(null, null);
            _keywordDeleteButton_Click(null, null);
        }

        private void _keywordListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex("^([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<string>(
                        (match.Groups[1].Value == "+") ? true : false,
                        match.Groups[2].Value
                    );

                    if (_keywordCollection.Contains(item)) continue;
                    _keywordCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _keywordListViewUpdate();
        }

        private void _keywordUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywordCollection.Move(selectIndex, selectIndex - 1);

            _keywordListViewUpdate();
        }

        private void _keywordDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywordCollection.Move(selectIndex, selectIndex + 1);

            _keywordListViewUpdate();
        }

        private void _keywordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;

            var item = new SearchContains<string>(
                _keywordContainsCheckBox.IsChecked.Value,
                _keywordTextBox.Text
            );

            if (_keywordCollection.Contains(item)) return;
            _keywordCollection.Add(item);

            _keywordListViewUpdate();
        }

        private void _keywordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;

            int selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            var item = new SearchContains<string>(
                _keywordContainsCheckBox.IsChecked.Value,
                _keywordTextBox.Text
            );

            if (_keywordCollection.Contains(item)) return;
            _keywordCollection.Set(selectIndex, item);

            _keywordListView.SelectedIndex = selectIndex;

            _keywordListViewUpdate();
        }

        private void _keywordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _keywordListView.SelectedItems.OfType<SearchContains<string>>().ToArray())
            {
                _keywordCollection.Remove(item);
            }

            _keywordListViewUpdate();
        }

        #endregion

        #region _creationTimeRangeListView

        private void _creationTimeRangeMinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_creationTimeRangeListView.SelectedIndex == -1)
                {
                    _creationTimeRangeAddButton_Click(null, null);
                }
                else
                {
                    _creationTimeRangeEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _creationTimeRangeMaxTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _creationTimeRangeAddButton_Click(null, null);
                _creationTimeRangeMinTextBox.Focus();

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _creationTimeRangeCollection.Count - 1)
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
                _creationTimeRangeContainsCheckBox.IsChecked = true;

                var maxDateTime = DateTime.Now.AddDays(1);

                _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);

                return;
            }

            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            _creationTimeRangeContainsCheckBox.IsChecked = item.Contains;
            _creationTimeRangeMinTextBox.Text = item.Value.Min.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = item.Value.Max.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        private void _creationTimeRangeListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _creationTimeRangeListView.SelectedItems;

            _creationTimeRangeListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _creationTimeRangeListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _creationTimeRangeListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], @"^([\+-]) (.*), (.*)$");
                }

                _creationTimeRangeListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _creationTimeRangeListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creationTimeRangeDeleteButton_Click(null, null);
        }

        private void _creationTimeRangeListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
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

        private void _creationTimeRangeListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creationTimeRangeListViewCopyMenuItem_Click(null, null);
            _creationTimeRangeDeleteButton_Click(null, null);
        }

        private void _creationTimeRangeListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex(@"^([\+-]) (.*), (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var min = DateTime.ParseExact(match.Groups[2].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();
                    var max = DateTime.ParseExact(match.Groups[3].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();

                    var item = new SearchContains<SearchRange<DateTime>>(
                        (match.Groups[1].Value == "+") ? true : false,
                        new SearchRange<DateTime>(min, max)
                    );

                    if (_creationTimeRangeCollection.Contains(item)) continue;
                    _creationTimeRangeCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _creationTimeRangeCollection.Move(selectIndex, selectIndex - 1);

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _creationTimeRangeCollection.Move(selectIndex, selectIndex + 1);

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            try
            {
                var min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();
                var max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();

                var item = new SearchContains<SearchRange<DateTime>>(
                    _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    new SearchRange<DateTime>(min, max)
                );

                if (_creationTimeRangeCollection.Contains(item)) return;
                _creationTimeRangeCollection.Add(item);
            }
            catch (Exception)
            {

            }

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            int selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();
                var max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();

                var item = new SearchContains<SearchRange<DateTime>>(
                    _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    new SearchRange<DateTime>(min, max)
                );

                if (_creationTimeRangeCollection.Contains(item)) return;
                _creationTimeRangeCollection.Set(selectIndex, item);

                _creationTimeRangeListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {

            }

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _creationTimeRangeListView.SelectedItems.OfType<SearchContains<SearchRange<DateTime>>>().ToArray())
            {
                _creationTimeRangeCollection.Remove(item);
            }

            _creationTimeRangeListViewUpdate();
        }

        #endregion

        #region _lengthRangeListView

        private void _lengthRangeMinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _lengthRangeMaxTextBox.Focus();

                e.Handled = true;
            }
        }

        private void _lengthRangeMaxTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _lengthRangeMinTextBox.Focus();

                if (_lengthRangeListView.SelectedIndex == -1)
                {
                    _lengthRangeAddButton_Click(null, null);
                }
                else
                {
                    _lengthRangeEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _lengthRangeCollection.Count - 1)
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
            _lengthRangeMinTextBox.Text = NetworkConverter.ToSizeString(item.Value.Min, "Byte");
            _lengthRangeMaxTextBox.Text = NetworkConverter.ToSizeString(item.Value.Max, "Byte");
        }

        private void _lengthRangeListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _lengthRangeListView.SelectedItems;

            _lengthRangeListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _lengthRangeListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _lengthRangeListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], @"^([\+-]) (.*), (.*)$");
                }

                _lengthRangeListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _lengthRangeListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _lengthRangeDeleteButton_Click(null, null);
        }

        private void _lengthRangeListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _lengthRangeListView.SelectedItems.OfType<SearchContains<SearchRange<long>>>())
            {
                sb.AppendLine(string.Format("{0} {1}, {2}", (item.Contains == true) ? "+" : "-", NetworkConverter.ToSizeString(item.Value.Min, "Byte"), NetworkConverter.ToSizeString(item.Value.Max, "Byte")));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _lengthRangeListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _lengthRangeListViewCopyMenuItem_Click(null, null);
            _lengthRangeDeleteButton_Click(null, null);
        }

        private void _lengthRangeListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex(@"^([\+-]) (.*), (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var min = Math.Max(0, (long)NetworkConverter.FromSizeString(match.Groups[2].Value));
                    var max = Math.Max(0, (long)NetworkConverter.FromSizeString(match.Groups[3].Value));

                    var item = new SearchContains<SearchRange<long>>(
                        (match.Groups[1].Value == "+") ? true : false,
                        new SearchRange<long>(min, max)
                    );

                    if (_lengthRangeCollection.Contains(item)) continue;
                    _lengthRangeCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            var selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _lengthRangeCollection.Move(selectIndex, selectIndex - 1);

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _lengthRangeListView.SelectedItem as SearchContains<SearchRange<long>>;
            if (item == null) return;

            var selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _lengthRangeCollection.Move(selectIndex, selectIndex + 1);

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lengthRangeMinTextBox.Text == "") return;
            if (_lengthRangeMaxTextBox.Text == "") return;

            try
            {
                var min = Math.Max(0, (long)NetworkConverter.FromSizeString(_lengthRangeMinTextBox.Text));
                var max = Math.Max(0, (long)NetworkConverter.FromSizeString(_lengthRangeMaxTextBox.Text));

                var item = new SearchContains<SearchRange<long>>(
                    _lengthRangeContainsCheckBox.IsChecked.Value,
                    new SearchRange<long>(min, max)
                );

                if (_lengthRangeCollection.Contains(item)) return;
                _lengthRangeCollection.Add(item);
            }
            catch (Exception)
            {

            }

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lengthRangeMinTextBox.Text == "") return;
            if (_lengthRangeMaxTextBox.Text == "") return;

            int selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var min = Math.Max(0, (long)NetworkConverter.FromSizeString(_lengthRangeMinTextBox.Text));
                var max = Math.Max(0, (long)NetworkConverter.FromSizeString(_lengthRangeMaxTextBox.Text));

                var item = new SearchContains<SearchRange<long>>(
                    _lengthRangeContainsCheckBox.IsChecked.Value,
                    new SearchRange<long>(min, max)
                );

                if (_lengthRangeCollection.Contains(item)) return;
                _lengthRangeCollection.Set(selectIndex, item);

                _lengthRangeListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {

            }

            _lengthRangeListViewUpdate();
        }

        private void _lengthRangeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _lengthRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _lengthRangeListView.SelectedItems.OfType<SearchContains<SearchRange<long>>>().ToArray())
            {
                _lengthRangeCollection.Remove(item);
            }

            _lengthRangeListViewUpdate();
        }

        #endregion

        #region _seedListView

        private void _seedTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_seedListView.SelectedIndex == -1)
                {
                    _seedAddButton_Click(null, null);
                }
                else
                {
                    _seedEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

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

                    if (selectIndex == _seedCollection.Count - 1)
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

                return;
            }

            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            _seedContainsCheckBox.IsChecked = item.Contains;
            _seedTextBox.Text = AmoebaConverter.ToSeedString(item.Value);
        }

        private void _seedListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _seedListView.SelectedItems;

            _seedListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _seedListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _seedListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], @"^([\+-]) (.*)$");
                }

                _seedListViewPasteMenuItem.IsEnabled = flag || Clipboard.ContainsSeeds();
            }
        }

        private void _seedListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _seedDeleteButton_Click(null, null);
        }

        private void _seedListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _seedListView.SelectedItems.OfType<SearchContains<Seed>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", AmoebaConverter.ToSeedString(item.Value)));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _seedListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _seedListViewCopyMenuItem_Click(null, null);
            _seedDeleteButton_Click(null, null);
        }

        private void _seedListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var seed in Clipboard.GetSeeds())
            {
                try
                {
                    var item = new SearchContains<Seed>(false, seed);

                    if (_seedCollection.Contains(item)) continue;
                    _seedCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            var regex = new Regex(@"^([\+-]) (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var seed = AmoebaConverter.FromSeedString(match.Groups[2].Value);
                    if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                    var item = new SearchContains<Seed>(
                        (match.Groups[1].Value == "+") ? true : false,
                        seed
                    );

                    if (_seedCollection.Contains(item)) continue;
                    _seedCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _seedListViewUpdate();
        }

        private void _seedUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            var selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            _seedCollection.Move(selectIndex, selectIndex - 1);

            _seedListViewUpdate();
        }

        private void _seedDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _seedListView.SelectedItem as SearchContains<Seed>;
            if (item == null) return;

            var selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            _seedCollection.Move(selectIndex, selectIndex + 1);

            _seedListViewUpdate();
        }

        private void _seedAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_seedTextBox.Text == "") return;

            try
            {
                var seed = AmoebaConverter.FromSeedString(_seedTextBox.Text);
                if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                var item = new SearchContains<Seed>(
                    _seedContainsCheckBox.IsChecked.Value,
                    seed
                );

                if (_seedCollection.Contains(item)) return;
                _seedCollection.Add(item);
            }
            catch (Exception)
            {

            }

            _seedListViewUpdate();
        }

        private void _seedEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_seedTextBox.Text == "") return;

            int selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            try
            {
                var seed = AmoebaConverter.FromSeedString(_seedTextBox.Text);
                if (!seed.VerifyCertificate()) seed.CreateCertificate(null);

                var item = new SearchContains<Seed>(
                    _seedContainsCheckBox.IsChecked.Value,
                    seed
                );

                if (_seedCollection.Contains(item)) return;
                _seedCollection.Set(selectIndex, item);

                _seedListView.SelectedIndex = selectIndex;
            }
            catch (Exception)
            {

            }

            _seedListViewUpdate();
        }

        private void _seedDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _seedListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _seedListView.SelectedItems.OfType<SearchContains<Seed>>().ToArray())
            {
                _seedCollection.Remove(item);
            }

            _seedListViewUpdate();
        }

        #endregion

        #region _stateListView

        private void _stateComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_stateListView.SelectedIndex == -1)
                {
                    _stateAddButton_Click(null, null);
                }
                else
                {
                    _stateEditButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _stateListViewUpdate()
        {
            _stateListView_SelectionChanged(this, null);
        }

        private void _stateListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _stateListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _stateUpButton.IsEnabled = false;
                    _stateDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _stateUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _stateUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _stateCollection.Count - 1)
                    {
                        _stateDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _stateDownButton.IsEnabled = true;
                    }
                }

                _stateListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _stateListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _stateListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _stateContainsCheckBox.IsChecked = true;
                _stateComboBox.SelectedIndex = -1;

                return;
            }

            var item = _stateListView.SelectedItem as SearchContains<SearchState>;
            if (item == null) return;

            _stateContainsCheckBox.IsChecked = item.Contains;
            _stateComboBox.SelectedItem = item.Value;
        }

        private void _stateListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _stateListView.SelectedItems;

            _stateListViewDeleteMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _stateListViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _stateListViewCutMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Regex.IsMatch(line[0], @"^([\+-]) (.*)$");
                }

                _stateListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _stateListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _stateDeleteButton_Click(null, null);
        }

        private void _stateListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _stateListView.SelectedItems.OfType<SearchContains<SearchState>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _stateListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _stateListViewCopyMenuItem_Click(null, null);
            _stateDeleteButton_Click(null, null);
        }

        private void _stateListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var regex = new Regex(@"^([\+-]) (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchState>(
                        (match.Groups[1].Value == "+") ? true : false,
                        (SearchState)Enum.Parse(typeof(SearchState), match.Groups[2].Value)
                    );

                    if (_stateCollection.Contains(item)) continue;
                    _stateCollection.Add(item);
                }
                catch (Exception)
                {

                }
            }

            _stateListViewUpdate();
        }

        private void _stateUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _stateListView.SelectedItem as SearchContains<SearchState>;
            if (item == null) return;

            var selectIndex = _stateListView.SelectedIndex;
            if (selectIndex == -1) return;

            _stateCollection.Move(selectIndex, selectIndex - 1);

            _stateListViewUpdate();
        }

        private void _stateDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _stateListView.SelectedItem as SearchContains<SearchState>;
            if (item == null) return;

            var selectIndex = _stateListView.SelectedIndex;
            if (selectIndex == -1) return;

            _stateCollection.Move(selectIndex, selectIndex + 1);

            _stateListViewUpdate();
        }

        private void _stateAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stateComboBox.SelectedIndex == -1) return;

            var item = new SearchContains<SearchState>(
                _stateContainsCheckBox.IsChecked.Value,
                (SearchState)_stateComboBox.SelectedItem
            );

            if (_stateCollection.Contains(item)) return;
            _stateCollection.Add(item);

            _stateListViewUpdate();
        }

        private void _stateEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stateComboBox.SelectedIndex == -1) return;

            int selectIndex = _stateListView.SelectedIndex;
            if (selectIndex == -1) return;

            var item = new SearchContains<SearchState>(
                _stateContainsCheckBox.IsChecked.Value,
                (SearchState)_stateComboBox.SelectedItem
            );

            if (_stateCollection.Contains(item)) return;
            _stateCollection.Set(selectIndex, item);

            _stateListView.SelectedIndex = selectIndex;

            _stateListViewUpdate();
        }

        private void _stateDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _stateListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _stateListView.SelectedItems.OfType<SearchContains<SearchState>>().ToArray())
            {
                _stateCollection.Remove(item);
            }

            _stateListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            lock (_searchItem.ThisLock)
            {
                _searchItem.Name = _searchTreeViewItemNameTextBox.Text;

                _searchItem.SearchNameCollection.Clear();
                _searchItem.SearchNameCollection.AddRange(_nameCollection);

                _searchItem.SearchNameRegexCollection.Clear();
                _searchItem.SearchNameRegexCollection.AddRange(_nameRegexCollection);

                _searchItem.SearchSignatureCollection.Clear();
                _searchItem.SearchSignatureCollection.AddRange(_signatureCollection);

                _searchItem.SearchKeywordCollection.Clear();
                _searchItem.SearchKeywordCollection.AddRange(_keywordCollection);

                _searchItem.SearchCreationTimeRangeCollection.Clear();
                _searchItem.SearchCreationTimeRangeCollection.AddRange(_creationTimeRangeCollection);

                _searchItem.SearchLengthRangeCollection.Clear();
                _searchItem.SearchLengthRangeCollection.AddRange(_lengthRangeCollection);

                _searchItem.SearchSeedCollection.Clear();
                _searchItem.SearchSeedCollection.AddRange(_seedCollection);

                _searchItem.SearchStateCollection.Clear();
                _searchItem.SearchStateCollection.AddRange(_stateCollection);
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_nameTabItem.IsSelected)
            {
                _nameListViewDeleteMenuItem_Click(null, null);
            }
            else if (_nameRegexTabItem.IsSelected)
            {
                _nameRegexListViewDeleteMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
            else if (_keywordTabItem.IsSelected)
            {
                _keywordListViewDeleteMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewDeleteMenuItem_Click(null, null);
            }
            else if (_lengthRangeTabItem.IsSelected)
            {
                _lengthRangeListViewDeleteMenuItem_Click(null, null);
            }
            else if (_seedTabItem.IsSelected)
            {
                _seedListViewDeleteMenuItem_Click(null, null);
            }
            else if (_stateTabItem.IsSelected)
            {
                _stateListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_nameTabItem.IsSelected)
            {
                _nameListViewCopyMenuItem_Click(null, null);
            }
            else if (_nameRegexTabItem.IsSelected)
            {
                _nameRegexListViewCopyMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewCopyMenuItem_Click(null, null);
            }
            else if (_keywordTabItem.IsSelected)
            {
                _keywordListViewCopyMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewCopyMenuItem_Click(null, null);
            }
            else if (_lengthRangeTabItem.IsSelected)
            {
                _lengthRangeListViewCopyMenuItem_Click(null, null);
            }
            else if (_seedTabItem.IsSelected)
            {
                _seedListViewCopyMenuItem_Click(null, null);
            }
            else if (_stateTabItem.IsSelected)
            {
                _stateListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_nameTabItem.IsSelected)
            {
                _nameListViewCutMenuItem_Click(null, null);
            }
            else if (_nameRegexTabItem.IsSelected)
            {
                _nameRegexListViewCutMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewCutMenuItem_Click(null, null);
            }
            else if (_keywordTabItem.IsSelected)
            {
                _keywordListViewCutMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewCutMenuItem_Click(null, null);
            }
            else if (_lengthRangeTabItem.IsSelected)
            {
                _lengthRangeListViewCutMenuItem_Click(null, null);
            }
            else if (_seedTabItem.IsSelected)
            {
                _seedListViewCutMenuItem_Click(null, null);
            }
            else if (_stateTabItem.IsSelected)
            {
                _stateListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_nameTabItem.IsSelected)
            {
                _nameListViewPasteMenuItem_Click(null, null);
            }
            else if (_nameRegexTabItem.IsSelected)
            {
                _nameRegexListViewPasteMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewPasteMenuItem_Click(null, null);
            }
            else if (_keywordTabItem.IsSelected)
            {
                _keywordListViewPasteMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewPasteMenuItem_Click(null, null);
            }
            else if (_lengthRangeTabItem.IsSelected)
            {
                _lengthRangeListViewPasteMenuItem_Click(null, null);
            }
            else if (_seedTabItem.IsSelected)
            {
                _seedListViewPasteMenuItem_Click(null, null);
            }
            else if (_stateTabItem.IsSelected)
            {
                _stateListViewPasteMenuItem_Click(null, null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using Amoeba.Properties;
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// KeywordWindow.xaml の相互作用ロジック
    /// </summary>
    partial class KeywordWindow : Window
    {
        private BufferManager _bufferManager = new BufferManager();

        private KeywordCollection _keywords = new KeywordCollection();

        public KeywordWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _keywords.AddRange(Settings.Instance.Global_SearchKeywords);

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _listView.ItemsSource = _keywords;
        }

        private void _listViewUpdate()
        {
            _listView_SelectionChanged(this, null);
        }

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _listView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _upButton.IsEnabled = false;
                    _downButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _upButton.IsEnabled = false;
                    }
                    else
                    {
                        _upButton.IsEnabled = true;
                    }

                    if (selectIndex == _keywords.Count - 1)
                    {
                        _downButton.IsEnabled = false;
                    }
                    else
                    {
                        _downButton.IsEnabled = true;
                    }
                }

                _listView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1)
            {
                _textBox.Text = "";

                return;
            }

            var item = _listView.SelectedItem as string;
            if (item == null) return;

            _textBox.Text = item;
        }

        private void _upButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords.Remove(item);
            _keywords.Insert(selectIndex - 1, item);
            _listView.Items.Refresh();

            _listViewUpdate();
        }

        private void _downButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords.Remove(item);
            _keywords.Insert(selectIndex + 1, item);
            _listView.Items.Refresh();

            _listViewUpdate();
        }

        private void _addButton_Click(object sender, RoutedEventArgs e)
        {
            if (_textBox.Text == "") return;
            if (string.IsNullOrWhiteSpace(_textBox.Text)) return;

            var keyword = _textBox.Text;
            if (_keywords.Contains(keyword)) return;
            _keywords.Add(keyword);

            _textBox.Text = "";
            _listView.SelectedIndex = _keywords.Count - 1;

            _listView.Items.Refresh();
            _listViewUpdate();
        }

        private void _editButton_Click(object sender, RoutedEventArgs e)
        {
            if (_textBox.Text == "") return;
            if (string.IsNullOrWhiteSpace(_textBox.Text)) return;

            var keyword = _textBox.Text;
            if (_keywords.Contains(keyword)) return;

            int selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords[selectIndex] = _textBox.Text;

            _listView.Items.Refresh();
            _listView.SelectedIndex = selectIndex;
            _listViewUpdate();
        }

        private void _deleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _textBox.Text = "";

            foreach (var item in _listView.SelectedItems.OfType<string>().ToArray())
            {
                _keywords.Remove(item);
            }

            _listView.Items.Refresh();
            _listView.SelectedIndex = selectIndex;
            _listViewUpdate();
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            Settings.Instance.Global_SearchKeywords.Clear();
            Settings.Instance.Global_SearchKeywords.AddRange(_keywords);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

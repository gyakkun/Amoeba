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
using Library.Net.Amoeba;
using Amoeba.Properties;
using Library;
using Library.Net;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// SignatureWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SignatureWindow : Window
    {
        private BufferManager _bufferManager = new BufferManager();
     
        private List<SignatureListViewItem> _signatureListViewItemCollection = new List<SignatureListViewItem>();

        public SignatureWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _signatureListViewItemCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.DeepClone())));

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _signatureListView.ItemsSource = _signatureListViewItemCollection;
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

                    if (selectIndex == _signatureListViewItemCollection.Count - 1)
                    {
                        _downButton.IsEnabled = false;
                    }
                    else
                    {
                        _downButton.IsEnabled = true;
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

        }

        private void _importButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.DefaultExt = ".sigunature";
                dialog.Filter = "Sigunature (*.sigunature)|*.sigunature";
                dialog.ShowDialog();

                foreach (var fileName in dialog.FileNames)
                {
                    using (FileStream stream = new FileStream(fileName, FileMode.Open))
                    {
                        try
                        {
                            var signature = AmoebaConverter.FromSignatureStream(stream);

                            _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));

                            _signatureListView.Items.Refresh();
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
        }

        private void _exportButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var signature = item.Value;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.FileName = MessageConverter.ToSignatureString(signature);
                dialog.DefaultExt = ".sigunature";
                dialog.Filter = "Sigunature (*.sigunature)|*.sigunature";
                dialog.ShowDialog();

                var fileName = dialog.FileName;

                using (FileStream stream = new FileStream(fileName, FileMode.Create))
                using (Stream signatureStream = AmoebaConverter.ToSignatureStream(signature))
                {
                    int i = -1;
                    byte[] buffer = _bufferManager.TakeBuffer(1024);

                    while ((i = signatureStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, i);
                    }

                    _bufferManager.ReturnBuffer(buffer);
                }
            }
        }

        private void _upButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex - 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _downButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex + 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _addButton_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(DigitalSignatureAlgorithm.ECDsa521_Sha512)));

            _signatureListView.Items.Refresh();
        }

        private void _deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            int selectIndex = _signatureListView.SelectedIndex;
            _signatureListViewItemCollection.Remove(item);
            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.Global_DigitalSignatureCollection.Clear();
            Settings.Instance.Global_DigitalSignatureCollection.AddRange(_signatureListViewItemCollection.Select(n => n.Value));

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private class SignatureListViewItem
        {
            private DigitalSignature _value;
            private string _text;

            public SignatureListViewItem(DigitalSignature signatureItem)
            {
                this.Value = signatureItem;
            }

            public void Update()
            {
                _text = MessageConverter.ToSignatureString(_value);
            }

            public DigitalSignature Value
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

            public string Text
            {
                get
                {
                    return _text;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Amoeba.Properties;
using Library.Net.Amoeba;
using Library.Net;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// UploadWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UploadWindow : Window
    {
        private AmoebaManager _amoebaManager;

        private string _filePath;
        private bool _isShare = false;

        public UploadWindow(string filePath, bool isShare, AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;
            _filePath = filePath;
            _isShare = isShare;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameTextBox.Text = System.IO.Path.GetFileName(_filePath);
            _keywordsComboBox.ItemsSource = Settings.Instance.Global_KeywordsCollection;
            _keywordsComboBox.SelectedIndex = 0;
            _signatureComboBox.ItemsSource = digitalSignatureCollection;
        }

        private void _keywordsComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var keywords = new KeywordCollection(_keywordsComboBox.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => new Keyword() { HashAlgorithm = HashAlgorithm.Sha512, Value = n })
                .Where(n => n.Value != null));

            _keywordsComboBox.Text = MessageConverter.ToKeywordsString(keywords);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            string name = _nameTextBox.Text;
            var keywords = new KeywordCollection(_keywordsComboBox.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => new Keyword() { HashAlgorithm = HashAlgorithm.Sha512, Value = n })
                .Where(n => n.Value != null));
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            if (!_isShare)
            {
                _amoebaManager.Upload(_filePath,
                    name,
                    keywords,
                    comment,
                    digitalSignature);
            }
            else
            {
                _amoebaManager.Share(_filePath,
                    name,
                    keywords,
                    comment,
                    digitalSignature);
            }

            var keywordString = MessageConverter.ToKeywordsString(keywords);

            if (keywordString != null)
            {
                Settings.Instance.Global_KeywordsCollection.Remove(keywordString);
                Settings.Instance.Global_KeywordsCollection.Insert(0, keywordString);
            }

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private class DigitalSignatureComboBoxItem : ComboBoxItem
        {
            private DigitalSignature _value;

            public DigitalSignatureComboBoxItem()
            {

            }

            public DigitalSignatureComboBoxItem(DigitalSignature digitalSignature)
            {
                this.Value = digitalSignature;
            }

            public void Update()
            {
                base.Content = MessageConverter.ToSignatureString(this.Value);
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
        }
    }
}

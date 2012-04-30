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
          
            if (Settings.Instance.Global_UploadKeywords.Count >= 1) _keywordsComboBox1.Text = Settings.Instance.Global_UploadKeywords[0];
            if (Settings.Instance.Global_UploadKeywords.Count >= 2) _keywordsComboBox2.Text = Settings.Instance.Global_UploadKeywords[1];
            if (Settings.Instance.Global_UploadKeywords.Count >= 3) _keywordsComboBox3.Text = Settings.Instance.Global_UploadKeywords[2];

            _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = "" });

            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = item.Value });
            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = item.Value });
            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = item.Value });

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            
            string name = _nameTextBox.Text;
            var keywords = new KeywordCollection();
            if (_keywordsComboBox1.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox1.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            if (_keywordsComboBox2.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox2.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            if (_keywordsComboBox3.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox3.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            keywords = new KeywordCollection(new HashSet<Keyword>(keywords));
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            if (!_isShare)
            {
                _amoebaManager.Upload(_filePath,
                    name,
                    keywords,
                    comment,
                    digitalSignature,
                    0);
            }
            else
            {
                _amoebaManager.Share(_filePath,
                    name,
                    keywords,
                    comment,
                    digitalSignature
                    ,0);
            }

            Settings.Instance.Global_UploadKeywords.Clear();
            Settings.Instance.Global_UploadKeywords.AddRange(keywords.Select(n => n.Value));
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

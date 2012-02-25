using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Library.Net.Amoeba;
using Amoeba.Properties;
using System.IO;
using Library.Net;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// SeedEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SeedEditWindow : Window
    {
        private Seed _seed;

        public SeedEditWindow(ref Seed seed)
        {
            _seed = seed;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameTextBox.Text = _seed.Name;
            var keywordsList = Settings.Instance.Global_KeywordsCollection.ToList();
            var keywords = MessageConverter.ToKeywordsString(_seed.Keywords);
            keywordsList.Remove(keywords);
            keywordsList.Insert(0, keywords);
            _keywordsComboBox.ItemsSource = keywordsList;
            _keywordsComboBox.SelectedIndex = 0;
            _commentTextBox.Text = _seed.Comment;
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
                .Where(n => n.Hash != null));
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _seed.Name = name;
            _seed.Keywords.Clear();
            _seed.Keywords.AddRange(keywords);
            _seed.Comment = comment;

            if (digitalSignature == null)
            {
                _seed.CreateCertificate(null);
            }
            else
            {
                _seed.CreateCertificate(digitalSignature);
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
                base.Content = new Label() { Content = MessageConverter.ToSignatureString(this.Value) };
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

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
        private AmoebaManager _amoebaManager;

        public SeedEditWindow(ref Seed seed, AmoebaManager amoebaManager)
        {
            _seed = seed;
            _amoebaManager = amoebaManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameTextBox.Text = _seed.Name;

            if (seed.Keywords.Count >= 1) _keywordsComboBox1.Text = seed.Keywords[0].Value;
            if (seed.Keywords.Count >= 2) _keywordsComboBox2.Text = seed.Keywords[1].Value;
            if (seed.Keywords.Count >= 3) _keywordsComboBox3.Text = seed.Keywords[2].Value;

            _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = "" });

            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = item.Value });
            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = item.Value });
            foreach (var item in _amoebaManager.SearchKeywords) _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = item.Value });

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            _commentTextBox.Text = _seed.Comment;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            string name = _nameTextBox.Text;
            var keywords = new KeywordCollection();
            if (_keywordsComboBox1.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox1.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            if (_keywordsComboBox2.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox2.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            if (_keywordsComboBox3.Text != "") keywords.Add(new Keyword() { Value = _keywordsComboBox3.Text, HashAlgorithm = Library.Net.Amoeba.HashAlgorithm.Sha512 });
            keywords = new KeywordCollection(new HashSet<Keyword>(keywords));
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

            Settings.Instance.Global_UploadKeywords.Clear();
            Settings.Instance.Global_UploadKeywords.AddRange(keywords.Select(n => n.Value));

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

    }

    class DigitalSignatureComboBoxItem : ComboBoxItem
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
            this.Content = MessageConverter.ToSignatureString(this.Value);
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

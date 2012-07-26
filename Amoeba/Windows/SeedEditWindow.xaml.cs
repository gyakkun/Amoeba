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
using System.Windows.Shapes;
using Amoeba.Properties;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// SeedEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SeedEditWindow : Window
    {
        private IList<Seed> _seeds;
        private AmoebaManager _amoebaManager;

        public SeedEditWindow(ref IList<Seed> seeds, AmoebaManager amoebaManager)
        {
            if (seeds.Count == 0) throw new ArgumentOutOfRangeException("seeds");

            _seeds = seeds;
            _amoebaManager = amoebaManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameTextBox.Text = _seeds[0].Name;

            foreach (var seed in _seeds)
            {
                if (_nameTextBox.Text != seed.Name)
                {
                    _nameTextBox.Text = "";
                    _nameTextBox.IsReadOnly = true;

                    break;
                }
            }

            if (_seeds[0].Keywords.Count >= 1) _keywordsComboBox1.Text = _seeds[0].Keywords[0];
            if (_seeds[0].Keywords.Count >= 2) _keywordsComboBox2.Text = _seeds[0].Keywords[1];
            if (_seeds[0].Keywords.Count >= 3) _keywordsComboBox3.Text = _seeds[0].Keywords[2];

            _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = "" });

            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = item });
            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = item });
            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = item });

            _commentTextBox.Text = _seeds[0].Comment;

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            
            var index = Settings.Instance.Global_DigitalSignatureCollection.IndexOf(Settings.Instance.Global_UploadDigitalSignature);
            _signatureComboBox.SelectedIndex = index + 1;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string name = _nameTextBox.Text;
            var keywords = new KeywordCollection();
            if (!string.IsNullOrWhiteSpace(_keywordsComboBox1.Text)) keywords.Add(_keywordsComboBox1.Text);
            if (!string.IsNullOrWhiteSpace(_keywordsComboBox2.Text)) keywords.Add(_keywordsComboBox2.Text);
            if (!string.IsNullOrWhiteSpace(_keywordsComboBox3.Text)) keywords.Add(_keywordsComboBox3.Text);
            keywords = new KeywordCollection(new HashSet<string>(keywords));
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            Settings.Instance.Global_UploadDigitalSignature = digitalSignature;

            var now = DateTime.UtcNow;

            foreach (var seed in _seeds)
            {
                lock (seed.ThisLock)
                {
                    if (!_nameTextBox.IsReadOnly)
                    {
                        seed.Name = name;
                    }

                    seed.Keywords.Clear();
                    seed.Keywords.AddRange(keywords);
                    seed.Comment = comment;
                    seed.CreationTime = now;

                    if (digitalSignature == null)
                    {
                        seed.CreateCertificate(null);
                    }
                    else
                    {
                        seed.CreateCertificate(digitalSignature);
                    }
                }
            }

            Settings.Instance.Global_UploadKeywords.Clear();
            Settings.Instance.Global_UploadKeywords.AddRange(keywords);
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

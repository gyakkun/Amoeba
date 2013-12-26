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
using Amoeba.Properties;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;
using Library;

namespace Amoeba.Windows
{
    /// <summary>
    /// SeedEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SeedEditWindow : Window
    {
        private List<Seed> _seeds;

        public SeedEditWindow(params Seed[] seeds)
        {
            if (seeds.Count() == 0) throw new ArgumentOutOfRangeException("seeds");

            _seeds = seeds.ToList();

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _nameTextBox.MaxLength = Seed.MaxNameLength;
            _commentTextBox.MaxLength = Seed.MaxCommentLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_seeds[0].ThisLock)
            {
                _nameTextBox.Text = _seeds[0].Name;

                if (_seeds.Count != 1)
                {
                    foreach (var seed in _seeds)
                    {
                        if (_nameTextBox.Text != seed.Name)
                        {
                            _nameTextBox.Text = "";
                            _nameTextBox.IsReadOnly = true;

                            break;
                        }
                    }
                }

                lock (_seeds[0].Keywords.ThisLock)
                {
                    if (_seeds[0].Keywords.Count >= 1) _keywordsComboBox1.Text = _seeds[0].Keywords[0];
                    if (_seeds[0].Keywords.Count >= 2) _keywordsComboBox2.Text = _seeds[0].Keywords[1];
                    if (_seeds[0].Keywords.Count >= 3) _keywordsComboBox3.Text = _seeds[0].Keywords[2];
                }

                _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = "" });
                _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = "" });
                _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = "" });

                foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = item });
                foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = item });
                foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = item });

                _commentTextBox.Text = _seeds[0].Comment;
            }

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            if (digitalSignatureCollection.Count > 0) _signatureComboBox.SelectedIndex = 1;

            _nameTextBox.TextChanged += _nameTextBox_TextChanged;
            _nameTextBox_TextChanged(null, null);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void _nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_nameTextBox.IsReadOnly == false)
            {
                _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
            }
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
            string comment = string.IsNullOrWhiteSpace(_commentTextBox.Text) ? null : _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            var now = DateTime.UtcNow;

            foreach (var seed in _seeds)
            {
                lock (seed.ThisLock)
                {
                    if (!_nameTextBox.IsReadOnly)
                    {
                        seed.Name = name;
                    }

                    lock (seed.Keywords.ThisLock)
                    {
                        seed.Keywords.Clear();
                        seed.Keywords.AddRange(keywords);
                    }

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
}

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
using Amoeba.Properties;
using Library.Net.Amoeba;
using Library.Net;
using Library.Security;
using System.Threading.Tasks;
using System.Threading;

namespace Amoeba.Windows
{
    /// <summary>
    /// UploadWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UploadWindow : Window
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;
        private AmoebaManager _amoebaManager;

        private ObservableCollectionEx<DigitalSignature> _digitalSignatureCollection = new ObservableCollectionEx<DigitalSignature>();

        private string _filePath;
        private bool _isShare;

        public UploadWindow(string filePath, bool isShare, AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;
            _filePath = filePath;
            _isShare = isShare;

            _digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatures.ToArray());

            InitializeComponent();

            _nameTextBox.MaxLength = Seed.MaxNameLength;
            _keywordsComboBox1.MaxLength = KeywordCollection.MaxKeywordLength;
            _keywordsComboBox2.MaxLength = KeywordCollection.MaxKeywordLength;
            _keywordsComboBox3.MaxLength = KeywordCollection.MaxKeywordLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _nameTextBox.Text = System.IO.Path.GetFileName(_filePath);

            if (Settings.Instance.Global_UploadKeywords.Count >= 1) _keywordsComboBox1.Text = Settings.Instance.Global_UploadKeywords[0];
            if (Settings.Instance.Global_UploadKeywords.Count >= 2) _keywordsComboBox2.Text = Settings.Instance.Global_UploadKeywords[1];
            if (Settings.Instance.Global_UploadKeywords.Count >= 3) _keywordsComboBox3.Text = Settings.Instance.Global_UploadKeywords[2];

            _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = "" });
            _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = "" });

            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox1.Items.Add(new ComboBoxItem() { Content = item });
            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox2.Items.Add(new ComboBoxItem() { Content = item });
            foreach (var item in Settings.Instance.Global_SearchKeywords) _keywordsComboBox3.Items.Add(new ComboBoxItem() { Content = item });

            _signatureComboBox_CollectionContainer.Collection = _digitalSignatureCollection;
            if (_digitalSignatureCollection.Count > 0) _signatureComboBox.SelectedIndex = 1;

            _nameTextBox_TextChanged(null, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);
        }

        private void _nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
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
            var digitalSignature = _signatureComboBox.SelectedItem as DigitalSignature;

            Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    if (!_isShare)
                    {
                        _amoebaManager.Upload(_filePath,
                                name,
                                keywords,
                                digitalSignature,
                                3);
                    }
                    else
                    {
                        _amoebaManager.Share(_filePath,
                                name,
                                keywords,
                                digitalSignature,
                                3);
                    }
                }
                catch (Exception)
                {

                }
            });

            Settings.Instance.Global_UploadKeywords.Clear();
            Settings.Instance.Global_UploadKeywords.AddRange(keywords);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

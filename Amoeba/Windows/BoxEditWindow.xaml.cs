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
using Library;

namespace Amoeba.Windows
{
    /// <summary>
    /// BoxEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class BoxEditWindow : Window
    {
        private List<Box> _boxes;

        public BoxEditWindow(params Box[] boxes)
        {
            _boxes = boxes.ToList();

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            lock (_boxes[0].ThisLock)
            {
                _nameTextBox.Text = _boxes[0].Name;

                foreach (var box in _boxes)
                {
                    if (_nameTextBox.Text != box.Name)
                    {
                        _nameTextBox.Text = "";
                        _nameTextBox.IsReadOnly = true;

                        break;
                    }
                }

                _commentTextBox.Text = _boxes[0].Comment;
            }

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            
            var index = Settings.Instance.Global_DigitalSignatureCollection.IndexOf(Settings.Instance.Global_UploadDigitalSignature);
            _signatureComboBox.SelectedIndex = index + 1;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string name = _nameTextBox.Text;
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            Settings.Instance.Global_UploadDigitalSignature = digitalSignature;

            var now = DateTime.UtcNow;

            foreach (var box in _boxes)
            {
                lock (box.ThisLock)
                {
                    if (!_nameTextBox.IsReadOnly)
                    {
                        box.Name = name;
                    }

                    box.Comment = comment;
                    box.CreationTime = now;

                    if (digitalSignature == null)
                    {
                        box.CreateCertificate(null);
                    }
                    else
                    {
                        box.CreateCertificate(digitalSignature);
                    }
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

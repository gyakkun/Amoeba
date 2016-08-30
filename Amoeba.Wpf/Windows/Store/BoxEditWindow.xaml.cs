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
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        private List<Box> _boxes;

        private ObservableCollectionEx<DigitalSignature> _digitalSignatureCollection = new ObservableCollectionEx<DigitalSignature>();

        public BoxEditWindow(params Box[] boxes)
            : this((IEnumerable<Box>)boxes)
        {

        }

        public BoxEditWindow(IEnumerable<Box> boxes)
        {
            _boxes = boxes.ToList();

            _digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.ToArray());

            InitializeComponent();

            _nameTextBox.MaxLength = Box.MaxNameLength;
            _commentTextBox.MaxLength = Box.MaxCommentLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_boxes[0].ThisLock)
            {
                _nameTextBox.Text = _boxes[0].Name;

                if (_boxes.Count != 1)
                {
                    foreach (var box in _boxes)
                    {
                        if (_nameTextBox.Text != box.Name)
                        {
                            _nameTextBox.Text = "";
                            _nameTextBox.IsReadOnly = true;

                            break;
                        }
                    }
                }

                _commentTextBox.Text = _boxes[0].Comment;
            }

            _signatureComboBox_CollectionContainer.Collection = _digitalSignatureCollection;
            if (_digitalSignatureCollection.Count > 0) _signatureComboBox.SelectedIndex = 1;

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
            string comment = string.IsNullOrWhiteSpace(_commentTextBox.Text) ? null : _commentTextBox.Text;
            var digitalSignature = _signatureComboBox.SelectedItem as DigitalSignature;

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

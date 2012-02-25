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
    /// BoxEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class BoxEditWindow : Window
    {
        private Box _box;

        public BoxEditWindow(ref Box box)
        {
            _box = box;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _nameTextBox.Text = _box.Name;
            _commentTextBox.Text = _box.Comment;
            _signatureComboBox.ItemsSource = digitalSignatureCollection;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            string name = _nameTextBox.Text;
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _box.Name = name;
            _box.Comment = comment;

            if (digitalSignature == null)
            {
                _box.CreateCertificate(null);
            }
            else
            {
                _box.CreateCertificate(digitalSignature);
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

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

namespace Amoeba.Windows
{
    /// <summary>
    /// SignatureWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SignatureWindow : Window
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        private DigitalSignature _digitalSignature;

        private ObservableCollectionEx<DigitalSignature> _digitalSignatureCollection = new ObservableCollectionEx<DigitalSignature>();

        public SignatureWindow()
            : this(null)
        {

        }

        public SignatureWindow(string signature)
        {
            _digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatures.ToArray());

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _signatureComboBox_CollectionContainer.Collection = _digitalSignatureCollection;
            if (_digitalSignatureCollection.Count > 0) _signatureComboBox.SelectedIndex = 1;

            if (signature != null)
            {
                for (int index = 0; index < Settings.Instance.Global_DigitalSignatures.Count; index++)
                {
                    if (Settings.Instance.Global_DigitalSignatures[index].ToString() == signature)
                    {
                        _signatureComboBox.SelectedIndex = index + 1;

                        break;
                    }
                }
            }
        }

        public DigitalSignature DigitalSignature
        {
            get
            {
                return _digitalSignature;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;

            WindowPosition.Move(this);
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _okButton.IsEnabled = (_signatureComboBox.SelectedItem is DigitalSignature);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            _digitalSignature = _signatureComboBox.SelectedItem as DigitalSignature;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

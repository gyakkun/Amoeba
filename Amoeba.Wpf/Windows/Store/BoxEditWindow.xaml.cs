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

        private Box _box;

        public BoxEditWindow(Box box)
        {
            _box = box;

            InitializeComponent();

            _nameTextBox.MaxLength = Box.MaxNameLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_box.ThisLock)
            {
                _nameTextBox.Text = _box.Name;
            }

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

            var now = DateTime.UtcNow;

            lock (_box.ThisLock)
            {
                if (!_nameTextBox.IsReadOnly)
                {
                    _box.Name = name;
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

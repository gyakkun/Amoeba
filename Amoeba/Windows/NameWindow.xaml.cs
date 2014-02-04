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
    /// NameWindow.xaml の相互作用ロジック
    /// </summary>
    partial class NameWindow : Window
    {
        private string _text;

        public NameWindow()
            : this(null)
        {

        }

        public NameWindow(int maxLength)
            : this(null)
        {
            _textBox.MaxLength = maxLength;
        }

        public NameWindow(string text)
        {
            _text = text;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _textBox.Text = _text;
        }

        public NameWindow(string text, int maxLength)
            : this(text)
        {
            _textBox.MaxLength = maxLength;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;

            this.SetBinding(Window.WindowStateProperty, new Binding("NameWindow_WindowState") { Mode = BindingMode.TwoWay, Source = Settings.Instance });
            WindowPosition.Move(this);
        }

        private void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_textBox.Text);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            _text = _textBox.Text;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

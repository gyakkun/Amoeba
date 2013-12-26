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
using System.IO;

namespace Amoeba.Windows
{
    /// <summary>
    /// ProgressWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ProgressWindow : Window
    {
        private bool _closeIsEnabled = true;

        public ProgressWindow(bool closeIsEnabled)
        {
            _closeIsEnabled = closeIsEnabled;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _button.IsEnabled = _closeIsEnabled;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_closeIsEnabled;
        }

        public string Message1
        {
            get
            {
                return (string)_label1.Content;
            }
            set
            {
                _label1.Content = value;
            }
        }

        public string Message2
        {
            get
            {
                return (string)_label2.Content;
            }
            set
            {
                _label2.Content = value;
            }
        }

        public string ButtonMessage
        {
            get
            {
                return (string)_button.Content;
            }
            set
            {
                _button.Content = value;
            }
        }

        public double? Value
        {
            get
            {
                return _progressBar.Value;
            }
            set
            {
                if (value == null)
                {
                    _progressBar.IsIndeterminate = true;
                }
                else
                {
                    _progressBar.IsIndeterminate = false;
                    _progressBar.Value = value.Value;
                }
            }
        }

        private void _button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

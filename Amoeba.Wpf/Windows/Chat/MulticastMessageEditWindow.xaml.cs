using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Library;
using Library.Collections;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;
using Amoeba;
using Amoeba.Properties;
using Library.Utilities;

namespace Amoeba.Windows
{
    /// <summary>
    /// MulticastMessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class MulticastMessageEditWindow : Window
    {
        private ServiceManager _serviceManager = ((App)Application.Current).ServiceManager;

        private Tag _tag;
        private AmoebaManager _amoebaManager;

        private WatchTimer _watchTimer;

        private AvalonEditHelper_MulticastMessage _textEditor_Helper = new AvalonEditHelper_MulticastMessage();
        private ObservableCollectionEx<DigitalSignature> _digitalSignatureCollection = new ObservableCollectionEx<DigitalSignature>();

        public MulticastMessageEditWindow(Tag tag, string comment, AmoebaManager amoebaManager)
        {
            _tag = tag;
            _amoebaManager = amoebaManager;

            _digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatures.ToArray());

            InitializeComponent();

            this.Title = LanguagesManager.Instance.MulticastMessageEditWindow_Title + " - " + MessageConverter.ToTagString(tag);

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(_serviceManager.Paths["Icons"], "Amoeba.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _signatureComboBox.ItemsSource = _digitalSignatureCollection;
            _signatureComboBox.SelectedIndex = 0;

            _commentTextBox.Text = comment;

            _watchTimer = new WatchTimer(this.WatchThread, 0, 1000);
            this.Closed += (sender, e) => _watchTimer.Dispose();

            _textEditor_Helper.Setup(_textEditor);
        }

        private void WatchThread()
        {
            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new TimeSpan(0, 0, 1), new Action(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_commentTextBox.Text) || _commentTextBox.Text.Length > Message.MaxCommentLength)
                    {
                        _okButton.IsEnabled = false;
                    }
                    else
                    {
                        _okButton.IsEnabled = true;
                    }

                    if (_commentTextBox.Text != null)
                    {
                        _countLabel.Content = string.Format("{0} / {1}", _commentTextBox.Text.Length, Message.MaxCommentLength);
                    }
                }
                catch (Exception)
                {

                }
            }));
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tabControl.SelectedItem == _previewTabItem)
            {
                if (string.IsNullOrWhiteSpace(_commentTextBox.Text))
                {
                    _textEditor.Document.Text = "";
                }
                else
                {
                    var digitalSignature = _signatureComboBox.SelectedItem as DigitalSignature;

                    var comment = _commentTextBox.Text.Substring(0, Math.Min(_commentTextBox.Text.Length, Message.MaxCommentLength));
                    _textEditor_Helper.Set(_textEditor, DateTime.UtcNow, digitalSignature.ToString(), 0, comment);
                }
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            var digitalSignature = _signatureComboBox.SelectedItem as DigitalSignature;

            int limit = 0;
            if (!Inspect.ContainTrustSignature(digitalSignature.ToString())) limit = -1;

            var comment = _commentTextBox.Text.Substring(0, Math.Min(_commentTextBox.Text.Length, Message.MaxCommentLength));
            var message = new Message(comment);

            _amoebaManager.MulticastUpload(_tag, message, limit, Settings.Instance.Global_MiningTime, digitalSignature);

            this.Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

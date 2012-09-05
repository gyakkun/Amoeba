using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Library;
using Library.Net.Amoeba;
using Amoeba.Properties;
using System.Runtime.Serialization;
using Library.Security;

namespace Amoeba.Windows
{
    /// <summary>
    /// UserInterfaceWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UserInterfaceWindow : Window
    {
        private BufferManager _bufferManager = new BufferManager();
        private KeywordCollection _keywords = new KeywordCollection();
        private List<SignatureListViewItem> _signatureListViewItemCollection = new List<SignatureListViewItem>();

        public UserInterfaceWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _keywords.AddRange(Settings.Instance.Global_SearchKeywords);
            _signatureListViewItemCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.DeepClone())));

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _keywordListView.ItemsSource = _keywords;

            _signatureListView.ItemsSource = _signatureListViewItemCollection;

            _updateUrlTextBox.Text = Settings.Instance.Global_Update_Url;
            _updateProxyUriTextBox.Text = Settings.Instance.Global_Update_ProxyUri;
            _updateSignatureTextBox.Text = Settings.Instance.Global_Update_Signature;

            if (Settings.Instance.Global_Update_Option == UpdateOption.None)
            {
                _updateOptionNoneRadioButton.IsChecked = true;
            }
            else if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck)
            {
                _updateOptionAutoCheckRadioButton.IsChecked = true;
            }
            else if (Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
            {
                _updateOptionAutoUpdateRadioButton.IsChecked = true;
            }

            try
            {
                string extension = ".box";
                string commandline = "\"" + Path.GetFullPath(Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe")) + "\" \"%1\"";
                string fileType = "Amoeba";
                string verb = "open";

                using (var regkey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (fileType != (string)regkey.GetValue("")) throw new Exception();
                }

                using (var shellkey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(fileType))
                {
                    using (var shellkey2 = shellkey.OpenSubKey("shell\\" + verb))
                    {
                        using (var shellkey3 = shellkey2.OpenSubKey("command"))
                        {
                            if (commandline != (string)shellkey3.GetValue("")) throw new Exception();
                        }
                    }
                }

                Settings.Instance.Global_RelateBoxFile_IsEnabled = true;
                _miscellaneousRelateBoxFileCheckBox.IsChecked = true;
            }
            catch
            {
                Settings.Instance.Global_RelateBoxFile_IsEnabled = false;
                _miscellaneousRelateBoxFileCheckBox.IsChecked = false;
            }
        }

        #region Keyword

        private void _keywordListViewUpdate()
        {
            _keywordListView_SelectionChanged(this, null);
        }

        private void _keywordListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _keywordListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _keywordUpButton.IsEnabled = false;
                    _keywordDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _keywordUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _keywordUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _keywords.Count - 1)
                    {
                        _keywordDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _keywordDownButton.IsEnabled = true;
                    }
                }

                _keywordListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _keywordListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _keywordTextBox.Text = "";

                return;
            }

            var item = _keywordListView.SelectedItem as string;
            if (item == null) return;

            _keywordTextBox.Text = item;
        }

        private void _keywordUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords.Remove(item);
            _keywords.Insert(selectIndex - 1, item);
            _keywordListView.Items.Refresh();

            _keywordListViewUpdate();
        }

        private void _keywordDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _keywordListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords.Remove(item);
            _keywords.Insert(selectIndex + 1, item);
            _keywordListView.Items.Refresh();

            _keywordListViewUpdate();
        }

        private void _keywordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;
            if (string.IsNullOrWhiteSpace(_keywordTextBox.Text)) return;

            var keyword = _keywordTextBox.Text;
            if (_keywords.Contains(keyword)) return;
            _keywords.Add(keyword);

            _keywordTextBox.Text = "";
            _keywordListView.SelectedIndex = _keywords.Count - 1;

            _keywordListView.Items.Refresh();
            _keywordListViewUpdate();
        }

        private void _keywordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keywordTextBox.Text == "") return;
            if (string.IsNullOrWhiteSpace(_keywordTextBox.Text)) return;

            var keyword = _keywordTextBox.Text;
            if (_keywords.Contains(keyword)) return;

            int selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywords[selectIndex] = _keywordTextBox.Text;

            _keywordListView.Items.Refresh();
            _keywordListView.SelectedIndex = selectIndex;
            _keywordListViewUpdate();
        }

        private void _keywordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _keywordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _keywordTextBox.Text = "";

            foreach (var item in _keywordListView.SelectedItems.OfType<string>().ToArray())
            {
                _keywords.Remove(item);
            }

            _keywordListView.Items.Refresh();
            _keywordListView.SelectedIndex = selectIndex;
            _keywordListViewUpdate();
        }

        #endregion

        #region Signature

        private void _signatureListViewUpdate()
        {
            _signatureListView_SelectionChanged(this, null);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _signatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _signatureUpButton.IsEnabled = false;
                    _signatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _signatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _signatureListViewItemCollection.Count - 1)
                    {
                        _signatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureDownButton.IsEnabled = true;
                    }
                }

                _signatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _signatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _signatureImportButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var fileName in dialog.FileNames)
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Open))
                        {
                            try
                            {
                                var signature = AmoebaConverter.FromSignatureStream(stream);
                                if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                                _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));

                                _signatureListView.Items.Refresh();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void _signatureExportButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var signature = item.Value;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.FileName = MessageConverter.ToSignatureString(signature);
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream signatureStream = AmoebaConverter.ToSignatureStream(signature))
                    {
                        int i = -1;
                        byte[] buffer = _bufferManager.TakeBuffer(1024);

                        while ((i = signatureStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, i);
                        }

                        _bufferManager.ReturnBuffer(buffer);
                    }
                }
            }
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex - 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex + 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_signatureTextBox.Text)) return;

            try
            {
                _signatureListViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(_signatureTextBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512)));

                _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;
                _signatureListView.Items.Refresh();
            }
            catch (Exception)
            {

            }
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            int selectIndex = _signatureListView.SelectedIndex;
            _signatureListViewItemCollection.Remove(item);
            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
        }

        #endregion

        #region Miscellaneous

        private void _miscellaneousStackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Expander expander = e.Source as Expander;
            if (expander == null) return;

            foreach (var item in _miscellaneousStackPanel.Children.OfType<Expander>())
            {
                if (expander != item) item.IsExpanded = false;
            }
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            Settings.Instance.Global_SearchKeywords.Clear();
            Settings.Instance.Global_SearchKeywords.AddRange(_keywords);

            Settings.Instance.Global_DigitalSignatureCollection.Clear();
            Settings.Instance.Global_DigitalSignatureCollection.AddRange(_signatureListViewItemCollection.Select(n => n.Value));

            Settings.Instance.Global_Update_Url = _updateUrlTextBox.Text;
            Settings.Instance.Global_Update_ProxyUri = _updateProxyUriTextBox.Text;
            Settings.Instance.Global_Update_Signature = _updateSignatureTextBox.Text;

            if (_updateOptionNoneRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.None;
            }
            else if (_updateOptionAutoCheckRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.AutoCheck;
            }
            else if (_updateOptionAutoUpdateRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.AutoUpdate;
            }

            if (Settings.Instance.Global_RelateBoxFile_IsEnabled != _miscellaneousRelateBoxFileCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_RelateBoxFile_IsEnabled = _miscellaneousRelateBoxFileCheckBox.IsChecked.Value;

                if (Settings.Instance.Global_RelateBoxFile_IsEnabled)
                {
                    System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo();
                    p.UseShellExecute = true;
                    p.FileName = Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe");
                    p.Arguments = "Relate on";

                    OperatingSystem osInfo = Environment.OSVersion;

                    if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version.Major >= 6)
                    {
                        p.Verb = "runas";
                    }

                    try
                    {
                        System.Diagnostics.Process.Start(p);
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {

                    }
                }
                else
                {
                    System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo();
                    p.UseShellExecute = true;
                    p.FileName = Path.Combine(App.DirectoryPaths["Core"], "Amoeba.exe");
                    p.Arguments = "Relate off";

                    OperatingSystem osInfo = Environment.OSVersion;

                    if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version.Major >= 6)
                    {
                        p.Verb = "runas";
                    }

                    try
                    {
                        System.Diagnostics.Process.Start(p);
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {

                    }
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private class SignatureListViewItem
        {
            private DigitalSignature _value;
            private string _text;

            public SignatureListViewItem(DigitalSignature signatureItem)
            {
                this.Value = signatureItem;
            }

            public void Update()
            {
                _text = MessageConverter.ToSignatureString(_value);
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

            public string Text
            {
                get
                {
                    return _text;
                }
            }
        }
    }

    [DataContract(Name = "UpdateOption", Namespace = "http://Amoeba/Windows")]
    enum UpdateOption
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "AutoCheck")]
        AutoCheck = 1,

        [EnumMember(Value = "AutoUpdate")]
        AutoUpdate = 2,
    }
}

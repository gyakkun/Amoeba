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

namespace Amoeba.Windows
{
    /// <summary>
    /// UserInterfaceWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UserInterfaceWindow : Window
    {
        public UserInterfaceWindow()
        {
            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Amoeba.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

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

            if ((Settings.Instance.Global_SearchFilterSettings_State & SearchState.Cache) == SearchState.Cache)
            {
                _miscellaneousSearchFilterCacheCheckBox.IsChecked = true;
            }
            if ((Settings.Instance.Global_SearchFilterSettings_State & SearchState.Uploading) == SearchState.Uploading)
            {
                _miscellaneousSearchFilterUploadingCheckBox.IsChecked = true;
            }
            if ((Settings.Instance.Global_SearchFilterSettings_State & SearchState.Downloading) == SearchState.Downloading)
            {
                _miscellaneousSearchFilterDownloadingCheckBox.IsChecked = true;
            }
            if ((Settings.Instance.Global_SearchFilterSettings_State & SearchState.Uploaded) == SearchState.Uploaded)
            {
                _miscellaneousSearchFilterUploadedCheckBox.IsChecked = true;
            }
            if ((Settings.Instance.Global_SearchFilterSettings_State & SearchState.Downloaded) == SearchState.Downloaded)
            {
                _miscellaneousSearchFilterDownloadedCheckBox.IsChecked = true;
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

            Settings.Instance.Global_SearchFilterSettings_State = 0;

            if (_miscellaneousSearchFilterCacheCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_SearchFilterSettings_State |= SearchState.Cache;
            }
            if (_miscellaneousSearchFilterUploadingCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_SearchFilterSettings_State |= SearchState.Uploading;
            }
            if (_miscellaneousSearchFilterDownloadingCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_SearchFilterSettings_State |= SearchState.Downloading;
            }
            if (_miscellaneousSearchFilterUploadedCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_SearchFilterSettings_State |= SearchState.Uploaded;
            }
            if (_miscellaneousSearchFilterDownloadedCheckBox.IsChecked.Value)
            {
                Settings.Instance.Global_SearchFilterSettings_State |= SearchState.Downloaded;
            }

            if (Settings.Instance.Global_RelateBoxFile_IsEnabled != _miscellaneousRelateBoxFileCheckBox.IsChecked.Value)
            {
                if (_miscellaneousRelateBoxFileCheckBox.IsChecked.Value)
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

                Settings.Instance.Global_RelateBoxFile_IsEnabled = _miscellaneousRelateBoxFileCheckBox.IsChecked.Value;
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
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

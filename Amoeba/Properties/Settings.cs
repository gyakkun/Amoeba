using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Amoeba.Windows;
using Library;
using Library.Collections;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static readonly Settings _defaultInstance = new Settings();
        private readonly object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingContent>()
            {
                new Library.Configuration.SettingContent<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_SearchKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_UploadKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConnectRunning", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConvertRunning", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_ConnectionSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_I2p_SamBridge_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_RelateBoxFile_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_OpenBox_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<string>() { Name = "Global_BoxExtractTo_Path", Value = "Box/Temp" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Amoeba" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:18118" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Signature", Value = "Lyrise@OTAhpWvmegu50LT-p5dZ16og7U6bdpO4z5TInZxGsCs" },
                new Library.Configuration.SettingContent<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },

                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "LinkOptionsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Grid_ColumnDefinitions_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_LinkerSignature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_TrustSignature_Width", Value = 120 },
                new Library.Configuration.SettingContent<LockedList<LinkItem>>() { Name = "LinkOptionsWindow_DownloadLinkItems", Value = new LockedList<LinkItem>() },
                new Library.Configuration.SettingContent<LockedList<LinkItem>>() { Name = "LinkOptionsWindow_UploadLinkItems", Value = new LockedList<LinkItem>() },

                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "OptionsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_BaseNode_Uris_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_OtherNodes_Node_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Client_Filters_GridViewColumn_Option_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Grid_ColumnDefinitions_Width", Value = 160 },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_BandwidthLimit_Unit", Value = "KB" },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_TransferLimit_Unit", Value = "GB" },
                new Library.Configuration.SettingContent<string>() { Name = "OptionsWindow_DataCacheSize_Unit", Value = "GB" },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Signature_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Keyword_GridViewColumn_Value_Width", Value = 400 },

                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "VersionInformationWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_GridViewColumn_FileName_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "VersionInformationWindow_GridViewColumn_Version_Width", Value = double.NaN },

                new Library.Configuration.SettingContent<string>() { Name = "InformationControl_LastHeaderClicked", Value = "Uri" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "InformationControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_Grid_ColumnDefinitions_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_Direction_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_Uri_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_ReceivedByteCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_SentByteCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_Name_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "InformationControl_GridViewColumn_Value_Width", Value = 100 },

                new Library.Configuration.SettingContent<Windows.SearchTreeItem>() { Name = "SearchControl_SearchTreeItem", Value = new Windows.SearchTreeItem(new Windows.SearchItem() { Name = "Search" }) },
                new Library.Configuration.SettingContent<string>() { Name = "SearchControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "SearchControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Keywords_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Comment_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "DownloadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "DownloadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Index_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Path_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "UploadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "UploadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Index_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Rate_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "ShareControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "ShareControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_Index_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_BlockCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_Path_Width", Value = 120 },

                new Library.Configuration.SettingContent<StoreCategorizeTreeItem>() { Name = "StoreDownloadControl_StoreCategorizeTreeItem", Value = new StoreCategorizeTreeItem() { Name = "Category" } },
                new Library.Configuration.SettingContent<string>() { Name = "StoreDownloadControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "StoreDownloadControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "StoreDownloadControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

                new Library.Configuration.SettingContent<StoreCategorizeTreeItem>() { Name = "StoreUploadControl_StoreCategorizeTreeItem", Value = new StoreCategorizeTreeItem() { Name = "Category" } },
                new Library.Configuration.SettingContent<string>() { Name = "StoreUploadControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "StoreUploadControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "StoreUploadControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

                new Library.Configuration.SettingContent<string>() { Name = "LibraryControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "LibraryControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<Box>() { Name = "LibraryControl_Box", Value = new Box() { Name = "Library" } },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "LibraryControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

                new Library.Configuration.SettingContent<double>() { Name = "ProgressWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "ProgressWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SignatureWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "NameWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SearchItemEditWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Name_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Name_Value_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width", Value = 400 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Keyword_Value_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Seed_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_Seed_Value_Width", Value = 600 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_GridViewColumn_SearchState_Value_Width", Value = 600 },

                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "UploadWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_GridViewColumn_Name_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_GridViewColumn_Path_Width", Value = double.NaN },

                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "BoxEditWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SeedEditWindow_WindowState", Value = WindowState.Normal },
            })
        {

        }

        public override void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                base.Load(directoryPath);
            }
        }

        public override void Save(string directoryPath)
        {
            lock (this.ThisLock)
            {
                base.Save(directoryPath);
            }
        }

        public static Settings Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        #region Property

        public LockedList<DigitalSignature> Global_DigitalSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<DigitalSignature>)this["Global_DigitalSignatureCollection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_DigitalSignatureCollection"] = value;
                }
            }
        }

        public LockedList<string> Global_SearchKeywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<string>)this["Global_SearchKeywords"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_SearchKeywords"] = value;
                }
            }
        }

        public LockedList<string> Global_UploadKeywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<string>)this["Global_UploadKeywords"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UploadKeywords"] = value;
                }
            }
        }

        public string Global_UseLanguage
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_UseLanguage"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UseLanguage"] = value;
                }
            }
        }

        public bool Global_IsConnectRunning
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_IsConnectRunning"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_IsConnectRunning"] = value;
                }
            }
        }

        public bool Global_IsConvertRunning
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_IsConvertRunning"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_IsConvertRunning"] = value;
                }
            }
        }

        public bool Global_ConnectionSetting_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_ConnectionSetting_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_ConnectionSetting_IsEnabled"] = value;
                }
            }
        }

        public bool Global_I2p_SamBridge_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_I2p_SamBridge_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_I2p_SamBridge_IsEnabled"] = value;
                }
            }
        }

        public bool Global_RelateBoxFile_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_RelateBoxFile_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_RelateBoxFile_IsEnabled"] = value;
                }
            }
        }

        public bool Global_OpenBox_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (bool)this["Global_OpenBox_IsEnabled"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_OpenBox_IsEnabled"] = value;
                }
            }
        }

        public string Global_BoxExtractTo_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_BoxExtractTo_Path"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_BoxExtractTo_Path"] = value;
                }
            }
        }

        public string Global_Update_Url
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_Url"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Url"] = value;
                }
            }
        }

        public string Global_Update_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_ProxyUri"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_ProxyUri"] = value;
                }
            }
        }

        public string Global_Update_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["Global_Update_Signature"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Signature"] = value;
                }
            }
        }

        public UpdateOption Global_Update_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (UpdateOption)this["Global_Update_Option"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Global_Update_Option"] = value;
                }
            }
        }


        public double MainWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Top"] = value;
                }
            }
        }

        public double MainWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Left"] = value;
                }
            }
        }

        public double MainWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Height"] = value;
                }
            }
        }

        public double MainWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["MainWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_Width"] = value;
                }
            }
        }

        public WindowState MainWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["MainWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["MainWindow_WindowState"] = value;
                }
            }
        }


        public double LinkOptionsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_Top"] = value;
                }
            }
        }

        public double LinkOptionsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_Left"] = value;
                }
            }
        }

        public double LinkOptionsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_Height"] = value;
                }
            }
        }

        public double LinkOptionsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_Width"] = value;
                }
            }
        }

        public WindowState LinkOptionsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["LinkOptionsWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_WindowState"] = value;
                }
            }
        }

        public double LinkOptionsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double LinkOptionsWindow_GridViewColumn_LinkerSignature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_GridViewColumn_LinkerSignature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_GridViewColumn_LinkerSignature_Width"] = value;
                }
            }
        }

        public double LinkOptionsWindow_GridViewColumn_TrustSignature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["LinkOptionsWindow_GridViewColumn_TrustSignature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_GridViewColumn_TrustSignature_Width"] = value;
                }
            }
        }

        public LockedList<LinkItem> LinkOptionsWindow_DownloadLinkItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<LinkItem>)this["LinkOptionsWindow_DownloadLinkItems"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_DownloadLinkItems"] = value;
                }
            }
        }

        public LockedList<LinkItem> LinkOptionsWindow_UploadLinkItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedList<LinkItem>)this["LinkOptionsWindow_UploadLinkItems"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LinkOptionsWindow_UploadLinkItems"] = value;
                }
            }
        }


        public double OptionsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Top"] = value;
                }
            }
        }

        public double OptionsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Left"] = value;
                }
            }
        }

        public double OptionsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Height"] = value;
                }
            }
        }

        public double OptionsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Width"] = value;
                }
            }
        }

        public WindowState OptionsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["OptionsWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_WindowState"] = value;
                }
            }
        }

        public double OptionsWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_BaseNode_Uris_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_OtherNodes_Node_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Client_Filters_GridViewColumn_Option_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public string OptionsWindow_BandwidthLimit_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_BandwidthLimit_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_BandwidthLimit_Unit"] = value;
                }
            }
        }

        public string OptionsWindow_TransferLimit_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_TransferLimit_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_TransferLimit_Unit"] = value;
                }
            }
        }

        public string OptionsWindow_DataCacheSize_Unit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["OptionsWindow_DataCacheSize_Unit"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_DataCacheSize_Unit"] = value;
                }
            }
        }

        public double OptionsWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Signature_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Signature_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double OptionsWindow_Keyword_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["OptionsWindow_Keyword_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["OptionsWindow_Keyword_GridViewColumn_Value_Width"] = value;
                }
            }
        }


        public double VersionInformationWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Top"] = value;
                }
            }
        }

        public double VersionInformationWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Left"] = value;
                }
            }
        }

        public double VersionInformationWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Height"] = value;
                }
            }
        }

        public double VersionInformationWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_Width"] = value;
                }
            }
        }

        public WindowState VersionInformationWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["VersionInformationWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_WindowState"] = value;
                }
            }
        }

        public double VersionInformationWindow_GridViewColumn_FileName_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_GridViewColumn_FileName_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_GridViewColumn_FileName_Width"] = value;
                }
            }
        }

        public double VersionInformationWindow_GridViewColumn_Version_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["VersionInformationWindow_GridViewColumn_Version_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["VersionInformationWindow_GridViewColumn_Version_Width"] = value;
                }
            }
        }


        public string InformationControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["InformationControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection InformationControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["InformationControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_ListSortDirection"] = value;
                }
            }
        }

        public double InformationControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_Direction_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_Direction_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_Direction_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_Uri_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_Priority_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_Priority_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_Priority_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_ReceivedByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_ReceivedByteCount_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_ReceivedByteCount_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_SentByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_SentByteCount_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_SentByteCount_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double InformationControl_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["InformationControl_GridViewColumn_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["InformationControl_GridViewColumn_Value_Width"] = value;
                }
            }
        }


        public Windows.SearchTreeItem SearchControl_SearchTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (Windows.SearchTreeItem)this["SearchControl_SearchTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_SearchTreeItem"] = value;
                }
            }
        }

        public string SearchControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["SearchControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection SearchControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["SearchControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_ListSortDirection"] = value;
                }
            }
        }

        public double SearchControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_Grid_ColumnDefinitions_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_Signature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Signature_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_State_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Keywords_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_Keywords_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Keywords_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_CreationTime_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_CreationTime_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_CreationTime_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_Length_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double SearchControl_GridViewColumn_Comment_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchControl_GridViewColumn_Comment_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_GridViewColumn_Comment_Width"] = value;
                }
            }
        }


        public string DownloadControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["DownloadControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection DownloadControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["DownloadControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_ListSortDirection"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Index_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Index_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Index_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_State_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Length_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Priority_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Priority_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Priority_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Rank_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Rank_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Rank_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Rate_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Rate_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Rate_Width"] = value;
                }
            }
        }

        public double DownloadControl_GridViewColumn_Path_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["DownloadControl_GridViewColumn_Path_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Path_Width"] = value;
                }
            }
        }


        public string UploadControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["UploadControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection UploadControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["UploadControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_ListSortDirection"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Index_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Index_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Index_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_State_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Length_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Priority_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Priority_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Priority_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Rank_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Rank_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Rank_Width"] = value;
                }
            }
        }

        public double UploadControl_GridViewColumn_Rate_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadControl_GridViewColumn_Rate_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Rate_Width"] = value;
                }
            }
        }


        public string ShareControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["ShareControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection ShareControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["ShareControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_ListSortDirection"] = value;
                }
            }
        }

        public double ShareControl_GridViewColumn_Index_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ShareControl_GridViewColumn_Index_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_GridViewColumn_Index_Width"] = value;
                }
            }
        }

        public double ShareControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ShareControl_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double ShareControl_GridViewColumn_BlockCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ShareControl_GridViewColumn_BlockCount_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_GridViewColumn_BlockCount_Width"] = value;
                }
            }
        }

        public double ShareControl_GridViewColumn_Path_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ShareControl_GridViewColumn_Path_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_GridViewColumn_Path_Width"] = value;
                }
            }
        }


        public StoreCategorizeTreeItem StoreDownloadControl_StoreCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (StoreCategorizeTreeItem)this["StoreDownloadControl_StoreCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreDownloadControl_StoreCategorizeTreeItem"] = value;
                }
            }
        }

        public string StoreDownloadControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["StoreDownloadControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreDownloadControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection StoreDownloadControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["StoreDownloadControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreDownloadControl_ListSortDirection"] = value;
                }
            }
        }

        public LockedHashSet<Route> StoreDownloadControl_ExpandedPaths
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<Route>)this["StoreDownloadControl_ExpandedPaths"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreDownloadControl_ExpandedPaths"] = value;
                }
            }
        }


        public StoreCategorizeTreeItem StoreUploadControl_StoreCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (StoreCategorizeTreeItem)this["StoreUploadControl_StoreCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreUploadControl_StoreCategorizeTreeItem"] = value;
                }
            }
        }

        public string StoreUploadControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["StoreUploadControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreUploadControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection StoreUploadControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["StoreUploadControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreUploadControl_ListSortDirection"] = value;
                }
            }
        }

        public LockedHashSet<Route> StoreUploadControl_ExpandedPaths
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<Route>)this["StoreUploadControl_ExpandedPaths"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["StoreUploadControl_ExpandedPaths"] = value;
                }
            }
        }


        public string LibraryControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (string)this["LibraryControl_LastHeaderClicked"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection LibraryControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (ListSortDirection)this["LibraryControl_ListSortDirection"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_ListSortDirection"] = value;
                }
            }
        }

        public Box LibraryControl_Box
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (Box)this["LibraryControl_Box"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_Box"] = value;
                }
            }
        }

        public LockedHashSet<Route> LibraryControl_ExpandedPaths
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashSet<Route>)this["LibraryControl_ExpandedPaths"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_ExpandedPaths"] = value;
                }
            }
        }


        public double ProgressWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["ProgressWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProgressWindow_Width"] = value;
                }
            }
        }

        public WindowState ProgressWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["ProgressWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["ProgressWindow_WindowState"] = value;
                }
            }
        }


        public double SignatureWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SignatureWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureWindow_Top"] = value;
                }
            }
        }

        public double SignatureWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SignatureWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureWindow_Left"] = value;
                }
            }
        }

        public double SignatureWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SignatureWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureWindow_Width"] = value;
                }
            }
        }

        public WindowState SignatureWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["SignatureWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SignatureWindow_WindowState"] = value;
                }
            }
        }


        public double NameWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["NameWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["NameWindow_Top"] = value;
                }
            }
        }

        public double NameWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["NameWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["NameWindow_Left"] = value;
                }
            }
        }

        public double NameWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["NameWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["NameWindow_Width"] = value;
                }
            }
        }

        public WindowState NameWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["NameWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["NameWindow_WindowState"] = value;
                }
            }
        }


        public double SearchItemEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_Top"] = value;
                }
            }
        }

        public double SearchItemEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_Left"] = value;
                }
            }
        }

        public double SearchItemEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_Height"] = value;
                }
            }
        }

        public double SearchItemEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_Width"] = value;
                }
            }
        }

        public WindowState SearchItemEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["SearchItemEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_WindowState"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Name_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Name_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Name_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Name_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Name_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Name_Value_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Signature_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Signature_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Keyword_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Keyword_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Keyword_Value_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Seed_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Seed_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Seed_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_Seed_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_Seed_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Seed_Value_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width"] = value;
                }
            }
        }

        public double SearchItemEditWindow_GridViewColumn_SearchState_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SearchItemEditWindow_GridViewColumn_SearchState_Value_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_SearchState_Value_Width"] = value;
                }
            }
        }


        public double UploadWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_Top"] = value;
                }
            }
        }

        public double UploadWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_Left"] = value;
                }
            }
        }

        public double UploadWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_Height"] = value;
                }
            }
        }

        public double UploadWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_Width"] = value;
                }
            }
        }

        public WindowState UploadWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["UploadWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_WindowState"] = value;
                }
            }
        }

        public double UploadWindow_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_GridViewColumn_Name_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double UploadWindow_GridViewColumn_Path_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["UploadWindow_GridViewColumn_Path_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["UploadWindow_GridViewColumn_Path_Width"] = value;
                }
            }
        }


        public double BoxEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["BoxEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["BoxEditWindow_Top"] = value;
                }
            }
        }

        public double BoxEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["BoxEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["BoxEditWindow_Left"] = value;
                }
            }
        }

        public double BoxEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["BoxEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["BoxEditWindow_Height"] = value;
                }
            }
        }

        public double BoxEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["BoxEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["BoxEditWindow_Width"] = value;
                }
            }
        }

        public WindowState BoxEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["BoxEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["BoxEditWindow_WindowState"] = value;
                }
            }
        }


        public double SeedEditWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SeedEditWindow_Top"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedEditWindow_Top"] = value;
                }
            }
        }

        public double SeedEditWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SeedEditWindow_Left"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedEditWindow_Left"] = value;
                }
            }
        }

        public double SeedEditWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SeedEditWindow_Height"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedEditWindow_Height"] = value;
                }
            }
        }

        public double SeedEditWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SeedEditWindow_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedEditWindow_Width"] = value;
                }
            }
        }

        public WindowState SeedEditWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (WindowState)this["SeedEditWindow_WindowState"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedEditWindow_WindowState"] = value;
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }
}

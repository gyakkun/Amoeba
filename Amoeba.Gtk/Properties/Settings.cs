using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
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
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_TrustSignatures", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<LockedList<LinkItem>>() { Name = "Global_LinkItems", Value = new LockedList<LinkItem>() },
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

                new Library.Configuration.SettingContent<LockedHashDictionary<string, LinkItem>>() { Name = "Cache_LinkItems", Value = new LockedHashDictionary<string, LinkItem>() },

                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "MainWindow_WindowState", Value = Gdk.WindowState.Maximized },

                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "LinkOptionsWindow_WindowState", Value = Gdk.WindowState.Sticky },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_Grid_ColumnDefinitions_Width", Value = 300 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_LinkerSignature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_TrustSignature_Width", Value = 120 },

                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "OptionsWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "OptionsWindow_WindowState", Value = Gdk.WindowState.Sticky },
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
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "VersionInformationWindow_WindowState", Value = Gdk.WindowState.Sticky },
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

                new Library.Configuration.SettingContent<double>() { Name = "LinkControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkControl_GridViewColumn_Signature_Width", Value = 600 },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "LinkControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

                new Library.Configuration.SettingContent<string>() { Name = "StoreReaderControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "StoreReaderControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<Box>() { Name = "StoreReaderControl_Box", Value = new Box() { Name = "Library" } },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "StoreReaderControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

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
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "ProgressWindow_WindowState", Value = Gdk.WindowState.Sticky },

                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SignatureWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "SignatureWindow_WindowState", Value = Gdk.WindowState.Sticky },

                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "NameWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "NameWindow_WindowState", Value = Gdk.WindowState.Sticky },

                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchItemEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "SearchItemEditWindow_WindowState", Value = Gdk.WindowState.Sticky },
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
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "UploadWindow_WindowState", Value = Gdk.WindowState.Sticky },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_GridViewColumn_Name_Width", Value = double.NaN },
                new Library.Configuration.SettingContent<double>() { Name = "UploadWindow_GridViewColumn_Path_Width", Value = double.NaN },

                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "BoxEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "BoxEditWindow_WindowState", Value = Gdk.WindowState.Sticky },

                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "SeedEditWindow_WindowState", Value = Gdk.WindowState.Sticky },

                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<Gdk.WindowState>() { Name = "SeedInformationWindow_WindowState", Value = Gdk.WindowState.Sticky },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_GridViewColumn_Signature_Width", Value = 600 },
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

        public double SeedInformationWindow_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (double)this["SeedInformationWindow_GridViewColumn_Signature_Width"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["SeedInformationWindow_GridViewColumn_Signature_Width"] = value;
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

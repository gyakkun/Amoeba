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
    class Settings : Library.Configuration.SettingsBase
    {
        private static readonly Settings _defaultInstance = new Settings();

        Settings()
            : base(new List<Library.Configuration.ISettingContent>()
            {
                new Library.Configuration.SettingContent<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatures", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_TrustSignatures", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<LockedList<LinkItem>>() { Name = "Global_LinkItems", Value = new LockedList<LinkItem>() },
                new Library.Configuration.SettingContent<LockedHashDictionary<string, ProfileSetting>>() { Name = "Global_ProfileSettings", Value = new LockedHashDictionary<string, ProfileSetting>() },
                new Library.Configuration.SettingContent<LockedHashSet<string>>() { Name = "Global_UrlHistorys", Value = new LockedHashSet<string>() },
                new Library.Configuration.SettingContent<LockedHashSet<Tag>>() { Name = "Global_TagHistorys", Value = new LockedHashSet<Tag>() },
                new Library.Configuration.SettingContent<LockedHashSet<Seed>>() { Name = "Global_SeedHistorys", Value = new LockedHashSet<Seed>() },
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_SearchKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_UploadKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConnectRunning", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConvertRunning", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_ConnectionSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_I2p_SamBridge_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Amoeba" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:18118" },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Update_Signature", Value = "Lyrise@CYA4hPfjHTj81-ItOlSGgYBEkSb7Zd7cTo_Qmxv5NnA" },
                new Library.Configuration.SettingContent<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.Check },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_RelateBoxFile_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_OpenBox_IsEnabled", Value = true },
                new Library.Configuration.SettingContent<string>() { Name = "Global_BoxExtractTo_Path", Value = "Box/Temp" },
                new Library.Configuration.SettingContent<int>() { Name = "Global_Limit", Value = 32 },
                new Library.Configuration.SettingContent<TimeSpan>() { Name = "Global_MiningTime", Value = new TimeSpan(0, 10, 0) },
                new Library.Configuration.SettingContent<string>() { Name = "Global_Fonts_Message_FontFamily", Value = "MS PGothic" },
                new Library.Configuration.SettingContent<double>() { Name = "Global_Fonts_Message_FontSize", Value = 12 },

                new Library.Configuration.SettingContent<LockedHashDictionary<string, Link>>() { Name = "Cache_Links", Value = new LockedHashDictionary<string, Link>() },
                new Library.Configuration.SettingContent<LockedHashDictionary<string, Profile>>() { Name = "Cache_Profiles", Value = new LockedHashDictionary<string, Profile>() },
                new Library.Configuration.SettingContent<LockedHashDictionary<string, Store>>() { Name = "Cache_Stores", Value = new LockedHashDictionary<string, Store>() },

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
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_YourSignature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkOptionsWindow_GridViewColumn_TrustSignature_Width", Value = 120 },

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

                new Library.Configuration.SettingContent<double>() { Name = "LinkControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "LinkControl_GridViewColumn_Signature_Width", Value = 600 },
                new Library.Configuration.SettingContent<LockedHashSet<Route>>() { Name = "LinkControl_ExpandedPaths", Value = new LockedHashSet<Route>() },

                new Library.Configuration.SettingContent<double>() { Name = "ChatControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<ChatCategorizeTreeItem>() { Name = "ChatControl_ChatCategorizeTreeItem", Value = new ChatCategorizeTreeItem(){ Name = "Category", IsExpanded = true } },

                new Library.Configuration.SettingContent<double>() { Name = "MulticastMessageEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MulticastMessageEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "MulticastMessageEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "MulticastMessageEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "MulticastMessageEditWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingContent<Windows.SearchTreeItem>() { Name = "SearchControl_SearchTreeItem", Value = new Windows.SearchTreeItem(new Windows.SearchItem() { Name = "Search" }) },
                new Library.Configuration.SettingContent<string>() { Name = "SearchControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "SearchControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_Keywords_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SearchControl_GridViewColumn_State_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "DownloadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "DownloadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_Path_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "DownloadControl_GridViewColumn_State_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "UploadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "UploadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_Path_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "UploadControl_GridViewColumn_State_Width", Value = 120 },

                new Library.Configuration.SettingContent<string>() { Name = "ShareControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "ShareControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_BlockCount_Width", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "ShareControl_GridViewColumn_Path_Width", Value = 120 },

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

                new Library.Configuration.SettingContent<Box>() { Name = "LibraryControl_Box", Value = new Box() { Name = "Library" } },
                new Library.Configuration.SettingContent<string>() { Name = "LibraryControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingContent<ListSortDirection>() { Name = "LibraryControl_ListSortDirection", Value = ListSortDirection.Ascending },
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

                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingContent<WindowState>() { Name = "SeedInformationWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingContent<double>() { Name = "SeedInformationWindow_GridViewColumn_Signature_Width", Value = 600 },
            })
        {

        }

        public static Settings Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        #region Property

        public LockedList<DigitalSignature> Global_DigitalSignatures { get { return (LockedList<DigitalSignature>)this["Global_DigitalSignatures"]; } set { this["Global_DigitalSignatures"] = value; } }
        public LockedList<string> Global_TrustSignatures { get { return (LockedList<string>)this["Global_TrustSignatures"]; } set { this["Global_TrustSignatures"] = value; } }
        public LockedList<LinkItem> Global_LinkItems { get { return (LockedList<LinkItem>)this["Global_LinkItems"]; } set { this["Global_LinkItems"] = value; } }
        public LockedHashDictionary<string, ProfileSetting> Global_ProfileSettings { get { return (LockedHashDictionary<string, ProfileSetting>)this["Global_ProfileSettings"]; } set { this["Global_ProfileSettings"] = value; } }
        public LockedHashSet<string> Global_UrlHistorys { get { return (LockedHashSet<string>)this["Global_UrlHistorys"]; } set { this["Global_UrlHistorys"] = value; } }
        public LockedHashSet<Tag> Global_TagHistorys { get { return (LockedHashSet<Tag>)this["Global_TagHistorys"]; } set { this["Global_TagHistorys"] = value; } }
        public LockedHashSet<Seed> Global_SeedHistorys { get { return (LockedHashSet<Seed>)this["Global_SeedHistorys"]; } set { this["Global_SeedHistorys"] = value; } }
        public LockedList<string> Global_SearchKeywords { get { return (LockedList<string>)this["Global_SearchKeywords"]; } set { this["Global_SearchKeywords"] = value; } }
        public LockedList<string> Global_UploadKeywords { get { return (LockedList<string>)this["Global_UploadKeywords"]; } set { this["Global_UploadKeywords"] = value; } }
        public string Global_UseLanguage { get { return (string)this["Global_UseLanguage"]; } set { this["Global_UseLanguage"] = value; } }
        public bool Global_IsConnectRunning { get { return (bool)this["Global_IsConnectRunning"]; } set { this["Global_IsConnectRunning"] = value; } }
        public bool Global_IsConvertRunning { get { return (bool)this["Global_IsConvertRunning"]; } set { this["Global_IsConvertRunning"] = value; } }
        public bool Global_ConnectionSetting_IsEnabled { get { return (bool)this["Global_ConnectionSetting_IsEnabled"]; } set { this["Global_ConnectionSetting_IsEnabled"] = value; } }
        public bool Global_I2p_SamBridge_IsEnabled { get { return (bool)this["Global_I2p_SamBridge_IsEnabled"]; } set { this["Global_I2p_SamBridge_IsEnabled"] = value; } }
        public string Global_Update_Url { get { return (string)this["Global_Update_Url"]; } set { this["Global_Update_Url"] = value; } }
        public string Global_Update_ProxyUri { get { return (string)this["Global_Update_ProxyUri"]; } set { this["Global_Update_ProxyUri"] = value; } }
        public string Global_Update_Signature { get { return (string)this["Global_Update_Signature"]; } set { this["Global_Update_Signature"] = value; } }
        public UpdateOption Global_Update_Option { get { return (UpdateOption)this["Global_Update_Option"]; } set { this["Global_Update_Option"] = value; } }
        public bool Global_RelateBoxFile_IsEnabled { get { return (bool)this["Global_RelateBoxFile_IsEnabled"]; } set { this["Global_RelateBoxFile_IsEnabled"] = value; } }
        public bool Global_OpenBox_IsEnabled { get { return (bool)this["Global_OpenBox_IsEnabled"]; } set { this["Global_OpenBox_IsEnabled"] = value; } }
        public string Global_BoxExtractTo_Path { get { return (string)this["Global_BoxExtractTo_Path"]; } set { this["Global_BoxExtractTo_Path"] = value; } }
        public int Global_Limit { get { return (int)this["Global_Limit"]; } set { this["Global_Limit"] = value; } }
        public TimeSpan Global_MiningTime { get { return (TimeSpan)this["Global_MiningTime"]; } set { this["Global_MiningTime"] = value; } }
        public string Global_Fonts_Message_FontFamily { get { return (string)this["Global_Fonts_Message_FontFamily"]; } set { this["Global_Fonts_Message_FontFamily"] = value; } }
        public double Global_Fonts_Message_FontSize { get { return (double)this["Global_Fonts_Message_FontSize"]; } set { this["Global_Fonts_Message_FontSize"] = value; } }

        public LockedHashDictionary<string, Link> Cache_Links { get { return (LockedHashDictionary<string, Link>)this["Cache_Links"]; } set { this["Cache_Links"] = value; } }
        public LockedHashDictionary<string, Profile> Cache_Profiles { get { return (LockedHashDictionary<string, Profile>)this["Cache_Profiles"]; } set { this["Cache_Profiles"] = value; } }
        public LockedHashDictionary<string, Store> Cache_Stores { get { return (LockedHashDictionary<string, Store>)this["Cache_Stores"]; } set { this["Cache_Stores"] = value; } }

        public double MainWindow_Top { get { return (double)this["MainWindow_Top"]; } set { this["MainWindow_Top"] = value; } }
        public double MainWindow_Left { get { return (double)this["MainWindow_Left"]; } set { this["MainWindow_Left"] = value; } }
        public double MainWindow_Height { get { return (double)this["MainWindow_Height"]; } set { this["MainWindow_Height"] = value; } }
        public double MainWindow_Width { get { return (double)this["MainWindow_Width"]; } set { this["MainWindow_Width"] = value; } }
        public WindowState MainWindow_WindowState { get { return (WindowState)this["MainWindow_WindowState"]; } set { this["MainWindow_WindowState"] = value; } }

        public double LinkOptionsWindow_Top { get { return (double)this["LinkOptionsWindow_Top"]; } set { this["LinkOptionsWindow_Top"] = value; } }
        public double LinkOptionsWindow_Left { get { return (double)this["LinkOptionsWindow_Left"]; } set { this["LinkOptionsWindow_Left"] = value; } }
        public double LinkOptionsWindow_Height { get { return (double)this["LinkOptionsWindow_Height"]; } set { this["LinkOptionsWindow_Height"] = value; } }
        public double LinkOptionsWindow_Width { get { return (double)this["LinkOptionsWindow_Width"]; } set { this["LinkOptionsWindow_Width"] = value; } }
        public WindowState LinkOptionsWindow_WindowState { get { return (WindowState)this["LinkOptionsWindow_WindowState"]; } set { this["LinkOptionsWindow_WindowState"] = value; } }
        public double LinkOptionsWindow_Grid_ColumnDefinitions_Width { get { return (double)this["LinkOptionsWindow_Grid_ColumnDefinitions_Width"]; } set { this["LinkOptionsWindow_Grid_ColumnDefinitions_Width"] = value; } }
        public double LinkOptionsWindow_GridViewColumn_Signature_Width { get { return (double)this["LinkOptionsWindow_GridViewColumn_Signature_Width"]; } set { this["LinkOptionsWindow_GridViewColumn_Signature_Width"] = value; } }
        public double LinkOptionsWindow_GridViewColumn_YourSignature_Width { get { return (double)this["LinkOptionsWindow_GridViewColumn_YourSignature_Width"]; } set { this["LinkOptionsWindow_GridViewColumn_YourSignature_Width"] = value; } }
        public double LinkOptionsWindow_GridViewColumn_TrustSignature_Width { get { return (double)this["LinkOptionsWindow_GridViewColumn_TrustSignature_Width"]; } set { this["LinkOptionsWindow_GridViewColumn_TrustSignature_Width"] = value; } }

        public double OptionsWindow_Top { get { return (double)this["OptionsWindow_Top"]; } set { this["OptionsWindow_Top"] = value; } }
        public double OptionsWindow_Left { get { return (double)this["OptionsWindow_Left"]; } set { this["OptionsWindow_Left"] = value; } }
        public double OptionsWindow_Height { get { return (double)this["OptionsWindow_Height"]; } set { this["OptionsWindow_Height"] = value; } }
        public double OptionsWindow_Width { get { return (double)this["OptionsWindow_Width"]; } set { this["OptionsWindow_Width"] = value; } }
        public WindowState OptionsWindow_WindowState { get { return (WindowState)this["OptionsWindow_WindowState"]; } set { this["OptionsWindow_WindowState"] = value; } }
        public double OptionsWindow_BaseNode_Uris_Uri_Width { get { return (double)this["OptionsWindow_BaseNode_Uris_Uri_Width"]; } set { this["OptionsWindow_BaseNode_Uris_Uri_Width"] = value; } }
        public double OptionsWindow_OtherNodes_Node_Width { get { return (double)this["OptionsWindow_OtherNodes_Node_Width"]; } set { this["OptionsWindow_OtherNodes_Node_Width"] = value; } }
        public double OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width { get { return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"]; } set { this["OptionsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value; } }
        public double OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width { get { return (double)this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"]; } set { this["OptionsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value; } }
        public double OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width { get { return (double)this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"]; } set { this["OptionsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value; } }
        public double OptionsWindow_Client_Filters_GridViewColumn_Option_Width { get { return (double)this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"]; } set { this["OptionsWindow_Client_Filters_GridViewColumn_Option_Width"] = value; } }
        public double OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width { get { return (double)this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"]; } set { this["OptionsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value; } }
        public double OptionsWindow_Grid_ColumnDefinitions_Width { get { return (double)this["OptionsWindow_Grid_ColumnDefinitions_Width"]; } set { this["OptionsWindow_Grid_ColumnDefinitions_Width"] = value; } }
        public string OptionsWindow_BandwidthLimit_Unit { get { return (string)this["OptionsWindow_BandwidthLimit_Unit"]; } set { this["OptionsWindow_BandwidthLimit_Unit"] = value; } }
        public string OptionsWindow_TransferLimit_Unit { get { return (string)this["OptionsWindow_TransferLimit_Unit"]; } set { this["OptionsWindow_TransferLimit_Unit"] = value; } }
        public string OptionsWindow_DataCacheSize_Unit { get { return (string)this["OptionsWindow_DataCacheSize_Unit"]; } set { this["OptionsWindow_DataCacheSize_Unit"] = value; } }
        public double OptionsWindow_Signature_GridViewColumn_Value_Width { get { return (double)this["OptionsWindow_Signature_GridViewColumn_Value_Width"]; } set { this["OptionsWindow_Signature_GridViewColumn_Value_Width"] = value; } }
        public double OptionsWindow_Keyword_GridViewColumn_Value_Width { get { return (double)this["OptionsWindow_Keyword_GridViewColumn_Value_Width"]; } set { this["OptionsWindow_Keyword_GridViewColumn_Value_Width"] = value; } }

        public double VersionInformationWindow_Top { get { return (double)this["VersionInformationWindow_Top"]; } set { this["VersionInformationWindow_Top"] = value; } }
        public double VersionInformationWindow_Left { get { return (double)this["VersionInformationWindow_Left"]; } set { this["VersionInformationWindow_Left"] = value; } }
        public double VersionInformationWindow_Height { get { return (double)this["VersionInformationWindow_Height"]; } set { this["VersionInformationWindow_Height"] = value; } }
        public double VersionInformationWindow_Width { get { return (double)this["VersionInformationWindow_Width"]; } set { this["VersionInformationWindow_Width"] = value; } }
        public WindowState VersionInformationWindow_WindowState { get { return (WindowState)this["VersionInformationWindow_WindowState"]; } set { this["VersionInformationWindow_WindowState"] = value; } }
        public double VersionInformationWindow_GridViewColumn_FileName_Width { get { return (double)this["VersionInformationWindow_GridViewColumn_FileName_Width"]; } set { this["VersionInformationWindow_GridViewColumn_FileName_Width"] = value; } }
        public double VersionInformationWindow_GridViewColumn_Version_Width { get { return (double)this["VersionInformationWindow_GridViewColumn_Version_Width"]; } set { this["VersionInformationWindow_GridViewColumn_Version_Width"] = value; } }

        public string InformationControl_LastHeaderClicked { get { return (string)this["InformationControl_LastHeaderClicked"]; } set { this["InformationControl_LastHeaderClicked"] = value; } }
        public ListSortDirection InformationControl_ListSortDirection { get { return (ListSortDirection)this["InformationControl_ListSortDirection"]; } set { this["InformationControl_ListSortDirection"] = value; } }
        public double InformationControl_Grid_ColumnDefinitions_Width { get { return (double)this["InformationControl_Grid_ColumnDefinitions_Width"]; } set { this["InformationControl_Grid_ColumnDefinitions_Width"] = value; } }
        public double InformationControl_GridViewColumn_Direction_Width { get { return (double)this["InformationControl_GridViewColumn_Direction_Width"]; } set { this["InformationControl_GridViewColumn_Direction_Width"] = value; } }
        public double InformationControl_GridViewColumn_Uri_Width { get { return (double)this["InformationControl_GridViewColumn_Uri_Width"]; } set { this["InformationControl_GridViewColumn_Uri_Width"] = value; } }
        public double InformationControl_GridViewColumn_Priority_Width { get { return (double)this["InformationControl_GridViewColumn_Priority_Width"]; } set { this["InformationControl_GridViewColumn_Priority_Width"] = value; } }
        public double InformationControl_GridViewColumn_ReceivedByteCount_Width { get { return (double)this["InformationControl_GridViewColumn_ReceivedByteCount_Width"]; } set { this["InformationControl_GridViewColumn_ReceivedByteCount_Width"] = value; } }
        public double InformationControl_GridViewColumn_SentByteCount_Width { get { return (double)this["InformationControl_GridViewColumn_SentByteCount_Width"]; } set { this["InformationControl_GridViewColumn_SentByteCount_Width"] = value; } }
        public double InformationControl_GridViewColumn_Name_Width { get { return (double)this["InformationControl_GridViewColumn_Name_Width"]; } set { this["InformationControl_GridViewColumn_Name_Width"] = value; } }
        public double InformationControl_GridViewColumn_Value_Width { get { return (double)this["InformationControl_GridViewColumn_Value_Width"]; } set { this["InformationControl_GridViewColumn_Value_Width"] = value; } }

        public double LinkControl_Grid_ColumnDefinitions_Width { get { return (double)this["LinkControl_Grid_ColumnDefinitions_Width"]; } set { this["LinkControl_Grid_ColumnDefinitions_Width"] = value; } }
        public double LinkControl_GridViewColumn_Signature_Width { get { return (double)this["LinkControl_GridViewColumn_Signature_Width"]; } set { this["LinkControl_GridViewColumn_Signature_Width"] = value; } }
        public LockedHashSet<Route> LinkControl_ExpandedPaths { get { return (LockedHashSet<Route>)this["LinkControl_ExpandedPaths"]; } set { this["LinkControl_ExpandedPaths"] = value; } }

        public double ChatControl_Grid_ColumnDefinitions_Width { get { return (double)this["ChatControl_Grid_ColumnDefinitions_Width"]; } set { this["ChatControl_Grid_ColumnDefinitions_Width"] = value; } }
        public ChatCategorizeTreeItem ChatControl_ChatCategorizeTreeItem { get { return (ChatCategorizeTreeItem)this["ChatControl_ChatCategorizeTreeItem"]; } set { this["ChatControl_ChatCategorizeTreeItem"] = value; } }

        public double MulticastMessageEditWindow_Top { get { return (double)this["MulticastMessageEditWindow_Top"]; } set { this["MulticastMessageEditWindow_Top"] = value; } }
        public double MulticastMessageEditWindow_Left { get { return (double)this["MulticastMessageEditWindow_Left"]; } set { this["MulticastMessageEditWindow_Left"] = value; } }
        public double MulticastMessageEditWindow_Height { get { return (double)this["MulticastMessageEditWindow_Height"]; } set { this["MulticastMessageEditWindow_Height"] = value; } }
        public double MulticastMessageEditWindow_Width { get { return (double)this["MulticastMessageEditWindow_Width"]; } set { this["MulticastMessageEditWindow_Width"] = value; } }
        public WindowState MulticastMessageEditWindow_WindowState { get { return (WindowState)this["MulticastMessageEditWindow_WindowState"]; } set { this["MulticastMessageEditWindow_WindowState"] = value; } }

        public Windows.SearchTreeItem SearchControl_SearchTreeItem { get { return (Windows.SearchTreeItem)this["SearchControl_SearchTreeItem"]; } set { this["SearchControl_SearchTreeItem"] = value; } }
        public string SearchControl_LastHeaderClicked { get { return (string)this["SearchControl_LastHeaderClicked"]; } set { this["SearchControl_LastHeaderClicked"] = value; } }
        public ListSortDirection SearchControl_ListSortDirection { get { return (ListSortDirection)this["SearchControl_ListSortDirection"]; } set { this["SearchControl_ListSortDirection"] = value; } }
        public double SearchControl_Grid_ColumnDefinitions_Width { get { return (double)this["SearchControl_Grid_ColumnDefinitions_Width"]; } set { this["SearchControl_Grid_ColumnDefinitions_Width"] = value; } }
        public double SearchControl_GridViewColumn_Name_Width { get { return (double)this["SearchControl_GridViewColumn_Name_Width"]; } set { this["SearchControl_GridViewColumn_Name_Width"] = value; } }
        public double SearchControl_GridViewColumn_Signature_Width { get { return (double)this["SearchControl_GridViewColumn_Signature_Width"]; } set { this["SearchControl_GridViewColumn_Signature_Width"] = value; } }
        public double SearchControl_GridViewColumn_Length_Width { get { return (double)this["SearchControl_GridViewColumn_Length_Width"]; } set { this["SearchControl_GridViewColumn_Length_Width"] = value; } }
        public double SearchControl_GridViewColumn_Keywords_Width { get { return (double)this["SearchControl_GridViewColumn_Keywords_Width"]; } set { this["SearchControl_GridViewColumn_Keywords_Width"] = value; } }
        public double SearchControl_GridViewColumn_CreationTime_Width { get { return (double)this["SearchControl_GridViewColumn_CreationTime_Width"]; } set { this["SearchControl_GridViewColumn_CreationTime_Width"] = value; } }
        public double SearchControl_GridViewColumn_State_Width { get { return (double)this["SearchControl_GridViewColumn_State_Width"]; } set { this["SearchControl_GridViewColumn_State_Width"] = value; } }

        public string DownloadControl_LastHeaderClicked { get { return (string)this["DownloadControl_LastHeaderClicked"]; } set { this["DownloadControl_LastHeaderClicked"] = value; } }
        public ListSortDirection DownloadControl_ListSortDirection { get { return (ListSortDirection)this["DownloadControl_ListSortDirection"]; } set { this["DownloadControl_ListSortDirection"] = value; } }
        public double DownloadControl_GridViewColumn_Name_Width { get { return (double)this["DownloadControl_GridViewColumn_Name_Width"]; } set { this["DownloadControl_GridViewColumn_Name_Width"] = value; } }
        public double DownloadControl_GridViewColumn_Length_Width { get { return (double)this["DownloadControl_GridViewColumn_Length_Width"]; } set { this["DownloadControl_GridViewColumn_Length_Width"] = value; } }
        public double DownloadControl_GridViewColumn_Priority_Width { get { return (double)this["DownloadControl_GridViewColumn_Priority_Width"]; } set { this["DownloadControl_GridViewColumn_Priority_Width"] = value; } }
        public double DownloadControl_GridViewColumn_Rate_Width { get { return (double)this["DownloadControl_GridViewColumn_Rate_Width"]; } set { this["DownloadControl_GridViewColumn_Rate_Width"] = value; } }
        public double DownloadControl_GridViewColumn_Path_Width { get { return (double)this["DownloadControl_GridViewColumn_Path_Width"]; } set { this["DownloadControl_GridViewColumn_Path_Width"] = value; } }
        public double DownloadControl_GridViewColumn_CreationTime_Width { get { return (double)this["DownloadControl_GridViewColumn_CreationTime_Width"]; } set { this["DownloadControl_GridViewColumn_CreationTime_Width"] = value; } }
        public double DownloadControl_GridViewColumn_State_Width { get { return (double)this["DownloadControl_GridViewColumn_State_Width"]; } set { this["DownloadControl_GridViewColumn_State_Width"] = value; } }

        public string UploadControl_LastHeaderClicked { get { return (string)this["UploadControl_LastHeaderClicked"]; } set { this["UploadControl_LastHeaderClicked"] = value; } }
        public ListSortDirection UploadControl_ListSortDirection { get { return (ListSortDirection)this["UploadControl_ListSortDirection"]; } set { this["UploadControl_ListSortDirection"] = value; } }
        public double UploadControl_GridViewColumn_Name_Width { get { return (double)this["UploadControl_GridViewColumn_Name_Width"]; } set { this["UploadControl_GridViewColumn_Name_Width"] = value; } }
        public double UploadControl_GridViewColumn_Length_Width { get { return (double)this["UploadControl_GridViewColumn_Length_Width"]; } set { this["UploadControl_GridViewColumn_Length_Width"] = value; } }
        public double UploadControl_GridViewColumn_Priority_Width { get { return (double)this["UploadControl_GridViewColumn_Priority_Width"]; } set { this["UploadControl_GridViewColumn_Priority_Width"] = value; } }
        public double UploadControl_GridViewColumn_Rate_Width { get { return (double)this["UploadControl_GridViewColumn_Rate_Width"]; } set { this["UploadControl_GridViewColumn_Rate_Width"] = value; } }
        public double UploadControl_GridViewColumn_Path_Width { get { return (double)this["UploadControl_GridViewColumn_Path_Width"]; } set { this["UploadControl_GridViewColumn_Path_Width"] = value; } }
        public double UploadControl_GridViewColumn_CreationTime_Width { get { return (double)this["UploadControl_GridViewColumn_CreationTime_Width"]; } set { this["UploadControl_GridViewColumn_CreationTime_Width"] = value; } }
        public double UploadControl_GridViewColumn_State_Width { get { return (double)this["UploadControl_GridViewColumn_State_Width"]; } set { this["UploadControl_GridViewColumn_State_Width"] = value; } }

        public string ShareControl_LastHeaderClicked { get { return (string)this["ShareControl_LastHeaderClicked"]; } set { this["ShareControl_LastHeaderClicked"] = value; } }
        public ListSortDirection ShareControl_ListSortDirection { get { return (ListSortDirection)this["ShareControl_ListSortDirection"]; } set { this["ShareControl_ListSortDirection"] = value; } }
        public double ShareControl_GridViewColumn_Name_Width { get { return (double)this["ShareControl_GridViewColumn_Name_Width"]; } set { this["ShareControl_GridViewColumn_Name_Width"] = value; } }
        public double ShareControl_GridViewColumn_BlockCount_Width { get { return (double)this["ShareControl_GridViewColumn_BlockCount_Width"]; } set { this["ShareControl_GridViewColumn_BlockCount_Width"] = value; } }
        public double ShareControl_GridViewColumn_Path_Width { get { return (double)this["ShareControl_GridViewColumn_Path_Width"]; } set { this["ShareControl_GridViewColumn_Path_Width"] = value; } }

        public string StoreReaderControl_LastHeaderClicked { get { return (string)this["StoreReaderControl_LastHeaderClicked"]; } set { this["StoreReaderControl_LastHeaderClicked"] = value; } }
        public ListSortDirection StoreReaderControl_ListSortDirection { get { return (ListSortDirection)this["StoreReaderControl_ListSortDirection"]; } set { this["StoreReaderControl_ListSortDirection"] = value; } }
        public Box StoreReaderControl_Box { get { return (Box)this["StoreReaderControl_Box"]; } set { this["StoreReaderControl_Box"] = value; } }
        public LockedHashSet<Route> StoreReaderControl_ExpandedPaths { get { return (LockedHashSet<Route>)this["StoreReaderControl_ExpandedPaths"]; } set { this["StoreReaderControl_ExpandedPaths"] = value; } }

        public StoreCategorizeTreeItem StoreDownloadControl_StoreCategorizeTreeItem { get { return (StoreCategorizeTreeItem)this["StoreDownloadControl_StoreCategorizeTreeItem"]; } set { this["StoreDownloadControl_StoreCategorizeTreeItem"] = value; } }
        public string StoreDownloadControl_LastHeaderClicked { get { return (string)this["StoreDownloadControl_LastHeaderClicked"]; } set { this["StoreDownloadControl_LastHeaderClicked"] = value; } }
        public ListSortDirection StoreDownloadControl_ListSortDirection { get { return (ListSortDirection)this["StoreDownloadControl_ListSortDirection"]; } set { this["StoreDownloadControl_ListSortDirection"] = value; } }
        public LockedHashSet<Route> StoreDownloadControl_ExpandedPaths { get { return (LockedHashSet<Route>)this["StoreDownloadControl_ExpandedPaths"]; } set { this["StoreDownloadControl_ExpandedPaths"] = value; } }

        public StoreCategorizeTreeItem StoreUploadControl_StoreCategorizeTreeItem { get { return (StoreCategorizeTreeItem)this["StoreUploadControl_StoreCategorizeTreeItem"]; } set { this["StoreUploadControl_StoreCategorizeTreeItem"] = value; } }
        public string StoreUploadControl_LastHeaderClicked { get { return (string)this["StoreUploadControl_LastHeaderClicked"]; } set { this["StoreUploadControl_LastHeaderClicked"] = value; } }
        public ListSortDirection StoreUploadControl_ListSortDirection { get { return (ListSortDirection)this["StoreUploadControl_ListSortDirection"]; } set { this["StoreUploadControl_ListSortDirection"] = value; } }
        public LockedHashSet<Route> StoreUploadControl_ExpandedPaths { get { return (LockedHashSet<Route>)this["StoreUploadControl_ExpandedPaths"]; } set { this["StoreUploadControl_ExpandedPaths"] = value; } }

        public Box LibraryControl_Box { get { return (Box)this["LibraryControl_Box"]; } set { this["LibraryControl_Box"] = value; } }
        public string LibraryControl_LastHeaderClicked { get { return (string)this["LibraryControl_LastHeaderClicked"]; } set { this["LibraryControl_LastHeaderClicked"] = value; } }
        public ListSortDirection LibraryControl_ListSortDirection { get { return (ListSortDirection)this["LibraryControl_ListSortDirection"]; } set { this["LibraryControl_ListSortDirection"] = value; } }
        public LockedHashSet<Route> LibraryControl_ExpandedPaths { get { return (LockedHashSet<Route>)this["LibraryControl_ExpandedPaths"]; } set { this["LibraryControl_ExpandedPaths"] = value; } }

        public double ProgressWindow_Width { get { return (double)this["ProgressWindow_Width"]; } set { this["ProgressWindow_Width"] = value; } }
        public WindowState ProgressWindow_WindowState { get { return (WindowState)this["ProgressWindow_WindowState"]; } set { this["ProgressWindow_WindowState"] = value; } }

        public double SignatureWindow_Top { get { return (double)this["SignatureWindow_Top"]; } set { this["SignatureWindow_Top"] = value; } }
        public double SignatureWindow_Left { get { return (double)this["SignatureWindow_Left"]; } set { this["SignatureWindow_Left"] = value; } }
        public double SignatureWindow_Width { get { return (double)this["SignatureWindow_Width"]; } set { this["SignatureWindow_Width"] = value; } }
        public WindowState SignatureWindow_WindowState { get { return (WindowState)this["SignatureWindow_WindowState"]; } set { this["SignatureWindow_WindowState"] = value; } }

        public double NameWindow_Top { get { return (double)this["NameWindow_Top"]; } set { this["NameWindow_Top"] = value; } }
        public double NameWindow_Left { get { return (double)this["NameWindow_Left"]; } set { this["NameWindow_Left"] = value; } }
        public double NameWindow_Width { get { return (double)this["NameWindow_Width"]; } set { this["NameWindow_Width"] = value; } }
        public WindowState NameWindow_WindowState { get { return (WindowState)this["NameWindow_WindowState"]; } set { this["NameWindow_WindowState"] = value; } }

        public double SearchItemEditWindow_Top { get { return (double)this["SearchItemEditWindow_Top"]; } set { this["SearchItemEditWindow_Top"] = value; } }
        public double SearchItemEditWindow_Left { get { return (double)this["SearchItemEditWindow_Left"]; } set { this["SearchItemEditWindow_Left"] = value; } }
        public double SearchItemEditWindow_Height { get { return (double)this["SearchItemEditWindow_Height"]; } set { this["SearchItemEditWindow_Height"] = value; } }
        public double SearchItemEditWindow_Width { get { return (double)this["SearchItemEditWindow_Width"]; } set { this["SearchItemEditWindow_Width"] = value; } }
        public WindowState SearchItemEditWindow_WindowState { get { return (WindowState)this["SearchItemEditWindow_WindowState"]; } set { this["SearchItemEditWindow_WindowState"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Name_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Name_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Name_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Name_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Name_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Name_Value_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Signature_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Signature_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Keyword_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Keyword_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Keyword_Value_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Seed_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Seed_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Seed_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_Seed_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_Seed_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_Seed_Value_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width"] = value; } }
        public double SearchItemEditWindow_GridViewColumn_SearchState_Value_Width { get { return (double)this["SearchItemEditWindow_GridViewColumn_SearchState_Value_Width"]; } set { this["SearchItemEditWindow_GridViewColumn_SearchState_Value_Width"] = value; } }

        public double UploadWindow_Top { get { return (double)this["UploadWindow_Top"]; } set { this["UploadWindow_Top"] = value; } }
        public double UploadWindow_Left { get { return (double)this["UploadWindow_Left"]; } set { this["UploadWindow_Left"] = value; } }
        public double UploadWindow_Height { get { return (double)this["UploadWindow_Height"]; } set { this["UploadWindow_Height"] = value; } }
        public double UploadWindow_Width { get { return (double)this["UploadWindow_Width"]; } set { this["UploadWindow_Width"] = value; } }
        public WindowState UploadWindow_WindowState { get { return (WindowState)this["UploadWindow_WindowState"]; } set { this["UploadWindow_WindowState"] = value; } }
        public double UploadWindow_GridViewColumn_Name_Width { get { return (double)this["UploadWindow_GridViewColumn_Name_Width"]; } set { this["UploadWindow_GridViewColumn_Name_Width"] = value; } }
        public double UploadWindow_GridViewColumn_Path_Width { get { return (double)this["UploadWindow_GridViewColumn_Path_Width"]; } set { this["UploadWindow_GridViewColumn_Path_Width"] = value; } }

        public double BoxEditWindow_Top { get { return (double)this["BoxEditWindow_Top"]; } set { this["BoxEditWindow_Top"] = value; } }
        public double BoxEditWindow_Left { get { return (double)this["BoxEditWindow_Left"]; } set { this["BoxEditWindow_Left"] = value; } }
        public double BoxEditWindow_Height { get { return (double)this["BoxEditWindow_Height"]; } set { this["BoxEditWindow_Height"] = value; } }
        public double BoxEditWindow_Width { get { return (double)this["BoxEditWindow_Width"]; } set { this["BoxEditWindow_Width"] = value; } }
        public WindowState BoxEditWindow_WindowState { get { return (WindowState)this["BoxEditWindow_WindowState"]; } set { this["BoxEditWindow_WindowState"] = value; } }

        public double SeedEditWindow_Top { get { return (double)this["SeedEditWindow_Top"]; } set { this["SeedEditWindow_Top"] = value; } }
        public double SeedEditWindow_Left { get { return (double)this["SeedEditWindow_Left"]; } set { this["SeedEditWindow_Left"] = value; } }
        public double SeedEditWindow_Height { get { return (double)this["SeedEditWindow_Height"]; } set { this["SeedEditWindow_Height"] = value; } }
        public double SeedEditWindow_Width { get { return (double)this["SeedEditWindow_Width"]; } set { this["SeedEditWindow_Width"] = value; } }
        public WindowState SeedEditWindow_WindowState { get { return (WindowState)this["SeedEditWindow_WindowState"]; } set { this["SeedEditWindow_WindowState"] = value; } }

        public double SeedInformationWindow_Top { get { return (double)this["SeedInformationWindow_Top"]; } set { this["SeedInformationWindow_Top"] = value; } }
        public double SeedInformationWindow_Left { get { return (double)this["SeedInformationWindow_Left"]; } set { this["SeedInformationWindow_Left"] = value; } }
        public double SeedInformationWindow_Height { get { return (double)this["SeedInformationWindow_Height"]; } set { this["SeedInformationWindow_Height"] = value; } }
        public double SeedInformationWindow_Width { get { return (double)this["SeedInformationWindow_Width"]; } set { this["SeedInformationWindow_Width"] = value; } }
        public WindowState SeedInformationWindow_WindowState { get { return (WindowState)this["SeedInformationWindow_WindowState"]; } set { this["SeedInformationWindow_WindowState"] = value; } }
        public double SeedInformationWindow_GridViewColumn_Signature_Width { get { return (double)this["SeedInformationWindow_GridViewColumn_Signature_Width"]; } set { this["SeedInformationWindow_GridViewColumn_Signature_Width"] = value; } }

        #endregion
    }
}

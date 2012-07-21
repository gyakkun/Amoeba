using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library;
using System.Windows;
using Library.Net;
using Amoeba.Windows;
using Library.Security;
using Library.Net.Amoeba;
using System.ComponentModel;

namespace Amoeba.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static Settings _defaultInstance = new Settings();
        private object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<List<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new List<DigitalSignature>() },
                new Library.Configuration.SettingsContext<DigitalSignature>() { Name = "Global_UploadDigitalSignature", Value = null },
                new Library.Configuration.SettingsContext<List<string>>() { Name = "Global_SearchKeywords", Value = new List<string>() },
                new Library.Configuration.SettingsContext<List<string>>() { Name = "Global_UploadKeywords", Value = new List<string>() },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<SearchState>() { Name = "Global_SearchFilterSettings_State", Value = 0 },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_RelateBoxFile_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Amoeba" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:8118" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Signature", Value = "Lyrise@iMK5aPkz6n_VLfaQWyXisi6C2yo53VbhMGTwJ4N2yGDTMXZwIdcZb8ayuGIOg-1V" },
                new Library.Configuration.SettingsContext<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },

                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ConnectionWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_BaseNode_Uris_Uri_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_OtherNodes_Node_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 600 },

                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "UserInterfaceWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Keyword_GridViewColumn_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "UserInterfaceWindow_Signature_GridViewColumn_Value_Width", Value = 600 },

                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "VersionInformationWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_GridViewColumn_FileName_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "VersionInformationWindow_GridViewColumn_Version_Width", Value = -1 },

                new Library.Configuration.SettingsContext<string>() { Name = "ConnectionControl_LastHeaderClicked", Value = "Uri" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "ConnectionControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_Grid_ColumnDefinitions_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Uri_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_ReceivedByteCount_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_SentByteCount_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Name_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionControl_GridViewColumn_Value_Width", Value = 100 },

                new Library.Configuration.SettingsContext<string>() { Name = "DownloadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "DownloadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Path_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Key_Width", Value = 120 },

                new Library.Configuration.SettingsContext<string>() { Name = "UploadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "UploadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Key_Width", Value = 120 },

                new Library.Configuration.SettingsContext<string>() { Name = "ShareControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "ShareControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_BlockCount_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_Path_Width", Value = 120 },
    
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "UploadWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_GridViewColumn_Name_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadWindow_GridViewColumn_Path_Width", Value = -1 },

                new Library.Configuration.SettingsContext<Windows.SearchTreeItem>() { Name = "CacheControl_SearchTreeItem", Value = new Windows.SearchTreeItem() { SearchItem = new Windows.SearchItem() { Name = "Search" } } },
                new Library.Configuration.SettingsContext<string>() { Name = "CacheControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "CacheControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Keywords_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Comment_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "CacheControl_GridViewColumn_Hash_Width", Value = 120 },

                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "SearchItemEditWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Name_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Name_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Value_IsIgnoreCase_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Keyword_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Keyword_Value_Width", Value = 600 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Max_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_CreationTimeRange_Value_Min_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Value_Max_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_LengthRange_Value_Min_Width", Value = 300 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Seed_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Seed_Value_Width", Value = 600 },

                new Library.Configuration.SettingsContext<string>() { Name = "LibraryControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "LibraryControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<Box>() { Name = "LibraryControl_Box", Value = new Box() { Name = "Library" } },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_Grid_ColumnDefinitions_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Signature_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Keywords_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_CreationTime_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Comment_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "LibraryControl_GridViewColumn_Key_Width", Value = 120 },

                new Library.Configuration.SettingsContext<double>() { Name = "BoxEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoxEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoxEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "BoxEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "BoxEditWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingsContext<double>() { Name = "SeedEditWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SeedEditWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SeedEditWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "SeedEditWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "SeedEditWindow_WindowState", Value = WindowState.Normal },
            })
        {

        }

        public override void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                try
                {
                    base.Load(directoryPath);
                }
                catch (Exception)
                {

                }
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

        public List<DigitalSignature> Global_DigitalSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (List<DigitalSignature>)this["Global_DigitalSignatureCollection"];
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

        public DigitalSignature Global_UploadDigitalSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (DigitalSignature)this["Global_UploadDigitalSignature"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_UploadDigitalSignature"] = value;
                }
            }
        }

        public List<string> Global_SearchKeywords
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (List<string>)this["Global_SearchKeywords"];
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

        public List<string> Global_UploadKeywords
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (List<string>)this["Global_UploadKeywords"];
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

        public bool Global_IsStart
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (bool)this["Global_IsStart"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_IsStart"] = value;
                }
            }
        }

        public bool Global_AutoBaseNodeSetting_IsEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (bool)this["Global_AutoBaseNodeSetting_IsEnabled"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_AutoBaseNodeSetting_IsEnabled"] = value;
                }
            }
        }

        public SearchState Global_SearchFilterSettings_State
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (SearchState)this["Global_SearchFilterSettings_State"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Global_SearchFilterSettings_State"] = value;
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


        public double ConnectionWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Top"] = value;
                }
            }
        }

        public double ConnectionWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Left"] = value;
                }
            }
        }

        public double ConnectionWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Height"] = value;
                }
            }
        }

        public double ConnectionWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Width"] = value;
                }
            }
        }

        public WindowState ConnectionWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ConnectionWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_WindowState"] = value;
                }
            }
        }

        public double ConnectionWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_BaseNode_Uris_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_OtherNodes_Node_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }


        public double UserInterfaceWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Top"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Left"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Height"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Width"] = value;
                }
            }
        }

        public WindowState UserInterfaceWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["UserInterfaceWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_WindowState"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Keyword_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Keyword_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Keyword_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double UserInterfaceWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UserInterfaceWindow_Signature_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UserInterfaceWindow_Signature_GridViewColumn_Value_Width"] = value;
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


        public string ConnectionControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["ConnectionControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection ConnectionControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["ConnectionControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_ListSortDirection"] = value;
                }
            }
        }

        public double ConnectionControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Priority_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Priority_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Priority_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_ReceivedByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_ReceivedByteCount_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_ReceivedByteCount_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_SentByteCount_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_SentByteCount_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_SentByteCount_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Name_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double ConnectionControl_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionControl_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionControl_GridViewColumn_Value_Width"] = value;
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

        public double DownloadControl_GridViewColumn_Key_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["DownloadControl_GridViewColumn_Key_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["DownloadControl_GridViewColumn_Key_Width"] = value;
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

        public double UploadControl_GridViewColumn_Key_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["UploadControl_GridViewColumn_Key_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["UploadControl_GridViewColumn_Key_Width"] = value;
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

        public double ShareControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ShareControl_GridViewColumn_Length_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ShareControl_GridViewColumn_Length_Width"] = value;
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


        public Windows.SearchTreeItem CacheControl_SearchTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (Windows.SearchTreeItem)this["CacheControl_SearchTreeItem"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_SearchTreeItem"] = value;
                }
            }
        }

        public string CacheControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["CacheControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection CacheControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["CacheControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_ListSortDirection"] = value;
                }
            }
        }

        public double CacheControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Name_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Signature_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Signature_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_State_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Keywords_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Keywords_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Keywords_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_CreationTime_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_CreationTime_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_CreationTime_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Length_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Comment_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Comment_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Comment_Width"] = value;
                }
            }
        }

        public double CacheControl_GridViewColumn_Hash_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["CacheControl_GridViewColumn_Hash_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["CacheControl_GridViewColumn_Hash_Width"] = value;
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

        public double SearchItemEditWindow_GridViewColumn_Signature_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["SearchItemEditWindow_GridViewColumn_Signature_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchItemEditWindow_GridViewColumn_Signature_Value_Width"] = value;
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

        public double LibraryControl_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Name_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Name_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Name_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Signature_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Signature_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Signature_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_State_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_State_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_State_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Keywords_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Keywords_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Keywords_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_CreationTime_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_CreationTime_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_CreationTime_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Length_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Length_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Length_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Comment_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Comment_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Comment_Width"] = value;
                }
            }
        }

        public double LibraryControl_GridViewColumn_Key_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["LibraryControl_GridViewColumn_Key_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["LibraryControl_GridViewColumn_Key_Width"] = value;
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

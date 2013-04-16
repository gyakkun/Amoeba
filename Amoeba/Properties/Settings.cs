using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using Amoeba.Windows;
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;
using Library.Collections;
using System.Windows.Media;

namespace Amoeba.Properties
{
    class Settings : Library.Configuration.SettingsBase, IThisLock
    {
        private static readonly Settings _defaultInstance = new Settings();
        private object _thisLock = new object();

        Settings()
            : base(new List<Library.Configuration.ISettingsContext>()
            {
                new Library.Configuration.SettingsContext<LockedList<DigitalSignature>>() { Name = "Global_DigitalSignatureCollection", Value = new LockedList<DigitalSignature>() },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UploadDigitalSignature", Value = null },
                new Library.Configuration.SettingsContext<LockedList<string>>() { Name = "Global_SearchKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingsContext<LockedList<string>>() { Name = "Global_UploadKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_IsStart", Value = true },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_AutoBaseNodeSetting_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_OpenBox_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<bool>() { Name = "Global_RelateBoxFile_IsEnabled", Value = true },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_BoxExtractTo_Path", Value = "Box/Temp" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Url", Value = "http://lyrise.web.fc2.com/update/Amoeba" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_ProxyUri", Value = "tcp:127.0.0.1:18118" },
                new Library.Configuration.SettingsContext<string>() { Name = "Global_Update_Signature", Value = "Lyrise@7seiSbhOCkls6gPxjJYjptxskzlSulgIe3dSfj1KxnJJ6eejKjuJ3R1Ec8yFuKpr4uNcwF7bFh5OrmxnY25y7A" },
                new Library.Configuration.SettingsContext<UpdateOption>() { Name = "Global_Update_Option", Value = UpdateOption.AutoCheck },

                new Library.Configuration.SettingsContext<Color>() { Name = "Color_Tree_Hit", Value = Colors.LightPink },

                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "MainWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "MainWindow_WindowState", Value = WindowState.Maximized },

                new Library.Configuration.SettingsContext<double>() { Name = "ProgressWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ProgressWindow_WindowState", Value = WindowState.Normal },

                new Library.Configuration.SettingsContext<double>() { Name = "SignatureWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SignatureWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "SignatureWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "SignatureWindow_WindowState", Value = WindowState.Normal },
              
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ConnectionsSettingsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_OtherNodes_Node_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Client_Filters_GridViewColumn_Option_Width", Value = 200 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width", Value = 160 },

                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Top", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Left", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Height", Value = 500 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Width", Value = 700 },
                new Library.Configuration.SettingsContext<WindowState>() { Name = "ViewSettingsWindow_WindowState", Value = WindowState.Normal },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Signature_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Keyword_GridViewColumn_Value_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "ViewSettingsWindow_Grid_ColumnDefinitions_Width", Value = 160 },

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

                new Library.Configuration.SettingsContext<LockedList<StoreInfo>>() { Name = "SearchControl_StoreTreeItems", Value = new LockedList<StoreInfo>() },
                new Library.Configuration.SettingsContext<string>() { Name = "SearchControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "SearchControl_ListSortDirection", Value = ListSortDirection.Ascending },

                new Library.Configuration.SettingsContext<string>() { Name = "DownloadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "DownloadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Index_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Rate_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "DownloadControl_GridViewColumn_Path_Width", Value = 120 },

                new Library.Configuration.SettingsContext<string>() { Name = "UploadControl_LastHeaderClicked", Value = "Rate" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "UploadControl_ListSortDirection", Value = ListSortDirection.Descending },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Index_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_State_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Length_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Priority_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Rank_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "UploadControl_GridViewColumn_Rate_Width", Value = 120 },

                new Library.Configuration.SettingsContext<string>() { Name = "ShareControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "ShareControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_Index_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_Name_Width", Value = 120 },
                new Library.Configuration.SettingsContext<double>() { Name = "ShareControl_GridViewColumn_BlockCount_Width", Value = 120 },
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
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_NameRegex_Value_Value_Width", Value = 400 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Value_IsIgnoreCase_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_Signature_Value_Value_Width", Value = 400 },
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
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_SearchState_Contains_Width", Value = -1 },
                new Library.Configuration.SettingsContext<double>() { Name = "SearchItemEditWindow_GridViewColumn_SearchState_Value_Width", Value = 600 },

                new Library.Configuration.SettingsContext<LockedList<StoreInfo>>() { Name = "StoreControl_StoreTreeItems", Value = new LockedList<StoreInfo>() },
                new Library.Configuration.SettingsContext<string>() { Name = "StoreControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "StoreControl_ListSortDirection", Value = ListSortDirection.Ascending },

                new Library.Configuration.SettingsContext<string>() { Name = "BoxControl_LastHeaderClicked", Value = "Name" },
                new Library.Configuration.SettingsContext<ListSortDirection>() { Name = "BoxControl_ListSortDirection", Value = ListSortDirection.Ascending },
                new Library.Configuration.SettingsContext<Box>() { Name = "BoxControl_Box", Value = new Box() { Name = "Box" } },
                new Library.Configuration.SettingsContext<Box>() { Name = "LibraryControl_Box", Value = new Box() { Name = "Library" } },

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

        public string Global_UploadDigitalSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["Global_UploadDigitalSignature"];
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


        public Color Color_Tree_Hit
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (Color)this["Color_Tree_Hit"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["Color_Tree_Hit"] = value;
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


        public double ConnectionsSettingsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Top"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Left"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Height"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Width"] = value;
                }
            }
        }

        public WindowState ConnectionsSettingsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ConnectionsSettingsWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_WindowState"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_BaseNode_Uris_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_OtherNodes_Node_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_OtherNodes_Node_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_OtherNodes_Node_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ConnectionType_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_ProxyUri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_UriCondition_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Client_Filters_GridViewColumn_Option_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_Option_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Client_Filters_GridViewColumn_Option_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Server_ListenUris_GridViewColumn_Uri_Width"] = value;
                }
            }
        }

        public double ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ConnectionsSettingsWindow_Grid_ColumnDefinitions_Width"] = value;
                }
            }
        }


        public double ViewSettingsWindow_Top
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Top"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Top"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Left
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Left"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Left"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Height
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Height"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Height"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Width"] = value;
                }
            }
        }

        public WindowState ViewSettingsWindow_WindowState
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (WindowState)this["ViewSettingsWindow_WindowState"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_WindowState"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Signature_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Signature_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Signature_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Keyword_GridViewColumn_Value_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Keyword_GridViewColumn_Value_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Keyword_GridViewColumn_Value_Width"] = value;
                }
            }
        }

        public double ViewSettingsWindow_Grid_ColumnDefinitions_Width
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (double)this["ViewSettingsWindow_Grid_ColumnDefinitions_Width"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["ViewSettingsWindow_Grid_ColumnDefinitions_Width"] = value;
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


        public LockedList<StoreInfo> SearchControl_StoreTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedList<StoreInfo>)this["SearchControl_StoreTreeItems"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["SearchControl_StoreTreeItems"] = value;
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


        public LockedList<StoreInfo> StoreControl_StoreTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (LockedList<StoreInfo>)this["StoreControl_StoreTreeItems"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["StoreControl_StoreTreeItems"] = value;
                }
            }
        }

        public string StoreControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["StoreControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["StoreControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection StoreControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["StoreControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["StoreControl_ListSortDirection"] = value;
                }
            }
        }


        public string BoxControl_LastHeaderClicked
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (string)this["BoxControl_LastHeaderClicked"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoxControl_LastHeaderClicked"] = value;
                }
            }
        }

        public ListSortDirection BoxControl_ListSortDirection
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (ListSortDirection)this["BoxControl_ListSortDirection"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoxControl_ListSortDirection"] = value;
                }
            }
        }

        public Box BoxControl_Box
        {
            get
            {
                lock (this.ThisLock)
                {
                   return (Box)this["BoxControl_Box"];
                }
            }

            set
            {
                lock (this.ThisLock)
                {
                    this["BoxControl_Box"] = value;
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

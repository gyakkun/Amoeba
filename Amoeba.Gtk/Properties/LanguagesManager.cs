using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Library;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Amoeba.Properties
{
    delegate void UsingLanguageChangedEventHandler(object sender);

    class LanguagesManager : IThisLock
    {
        private static LanguagesManager _defaultInstance = new LanguagesManager();
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _currentLanguage;
        private readonly object _thisLock = new object();

        public static UsingLanguageChangedEventHandler UsingLanguageChangedEvent;

        protected static void OnUsingLanguageChangedEvent()
        {
            LanguagesManager.UsingLanguageChangedEvent?.Invoke(_defaultInstance);
        }

        static LanguagesManager()
        {
#if DEBUG
            string path = @"C:/Local/Projects/Alliance-Network/Amoeba/Amoeba/bin/Debug/Core/Languages";

            if (!Directory.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
#else
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Languages");
#endif

            LanguagesManager.Load(path);
        }

        private static void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var dic = new Dictionary<string, string>();

                using (XmlTextReader xml = new XmlTextReader(path))
                {
                    try
                    {
                        while (xml.Read())
                        {
                            if (xml.NodeType == XmlNodeType.Element)
                            {
                                if (xml.LocalName == "Translate")
                                {
                                    dic.Add(xml.GetAttribute("Key"), xml.GetAttribute("Value"));
                                }
                            }
                        }
                    }
                    catch (XmlException)
                    {

                    }
                }

                _dic[Path.GetFileNameWithoutExtension(path)] = dic;
            }

            if (CultureInfo.CurrentUICulture.Name == "ja-JP" && _dic.Keys.Any(n => n == "Japanese"))
            {
                _currentLanguage = "Japanese";
            }
            else if (_dic.Keys.Any(n => n == "English"))
            {
                _currentLanguage = "English";
            }
        }

        public static LanguagesManager Instance
        {
            get
            {
                return _defaultInstance;
            }
        }

        /// <summary>
        /// 言語の切り替えメソッド
        /// </summary>
        /// <param name="language">使用言語を指定する</param>
        public static void ChangeLanguage(string language)
        {
            if (!_dic.ContainsKey(language)) throw new ArgumentException();

            _currentLanguage = language;
            LanguagesManager.OnUsingLanguageChangedEvent();
        }

        /// <summary>
        /// 使用できる言語リスト
        /// </summary>
        public IEnumerable<string> Languages
        {
            get
            {
                var dic = new Dictionary<string, string>();

                foreach (var path in _dic.Keys.ToList())
                {
                    dic[System.IO.Path.GetFileNameWithoutExtension(path)] = path;
                }

                var pairs = dic.ToList();

                pairs.Sort((x, y) =>
                {
                    return x.Key.CompareTo(y.Key);
                });

                return pairs.Select(n => n.Value).ToArray();
            }
        }

        /// <summary>
        /// 現在使用している言語
        /// </summary>
        public string CurrentLanguage
        {
            get
            {
                return _currentLanguage;
            }
        }

        public string Translate(string key)
        {
            if (_currentLanguage == null) return null;

            string result;

            if (_dic[_currentLanguage].TryGetValue(key, out result))
            {
                return Regex.Unescape(result);
            }

            return null;
        }

        #region Property

        public string FontFamily
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("FontFamily");
                }
            }
        }

        public string FontSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("FontSize");
                }
            }
        }

        public string DateTime_StringFormat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DateTime_StringFormat");
                }
            }
        }


        public string Languages_English
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_English");
                }
            }
        }

        public string Languages_Japanese
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Japanese");
                }
            }
        }

        public string Languages_Chinese_Traditional
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Chinese_Traditional");
                }
            }
        }

        public string Languages_Chinese_Simplified
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Chinese_Simplified");
                }
            }
        }

        public string Languages_Korean
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Korean");
                }
            }
        }

        public string Languages_Ukrainian
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Ukrainian");
                }
            }
        }

        public string Languages_Russian
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Languages_Russian");
                }
            }
        }


        public string ConnectDirection_In
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectDirection_In");
                }
            }
        }

        public string ConnectDirection_Out
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectDirection_Out");
                }
            }
        }


        public string Seed_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Name");
                }
            }
        }

        public string Seed_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Signature");
                }
            }
        }

        public string Seed_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Length");
                }
            }
        }

        public string Seed_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Keywords");
                }
            }
        }

        public string Seed_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_CreationTime");
                }
            }
        }

        public string Seed_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Seed_Comment");
                }
            }
        }


        public string Box_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Name");
                }
            }
        }

        public string Box_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Signature");
                }
            }
        }

        public string Box_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_CreationTime");
                }
            }
        }

        public string Box_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("Box_Comment");
                }
            }
        }


        public string MainWindow_Information
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Information");
                }
            }
        }

        public string MainWindow_Search
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Search");
                }
            }
        }

        public string MainWindow_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Download");
                }
            }
        }

        public string MainWindow_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Upload");
                }
            }
        }

        public string MainWindow_Share
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Share");
                }
            }
        }

        public string MainWindow_Link
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Link");
                }
            }
        }

        public string MainWindow_Store
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Store");
                }
            }
        }

        public string MainWindow_Log
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Log");
                }
            }
        }

        public string MainWindow_StatesBar
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_StatesBar");
                }
            }
        }

        public string MainWindow_Running
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Running");
                }
            }
        }

        public string MainWindow_Stopping
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Stopping");
                }
            }
        }

        public string MainWindow_Core
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Core");
                }
            }
        }

        public string MainWindow_ConnectStart
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConnectStart");
                }
            }
        }

        public string MainWindow_ConnectStop
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConnectStop");
                }
            }
        }

        public string MainWindow_UpdateBaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_UpdateBaseNode");
                }
            }
        }

        public string MainWindow_Cache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Cache");
                }
            }
        }

        public string MainWindow_ConvertStart
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConvertStart");
                }
            }
        }

        public string MainWindow_ConvertStop
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ConvertStop");
                }
            }
        }

        public string MainWindow_CheckInternalBlocks
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckInternalBlocks");
                }
            }
        }

        public string MainWindow_CheckExternalBlocks
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckExternalBlocks");
                }
            }
        }

        public string MainWindow_Tools
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Tools");
                }
            }
        }

        public string MainWindow_LinkOptions
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_LinkOptions");
                }
            }
        }

        public string MainWindow_Options
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Options");
                }
            }
        }

        public string MainWindow_Languages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Languages");
                }
            }
        }

        public string MainWindow_Help
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Help");
                }
            }
        }

        public string MainWindow_ManualSite
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ManualSite");
                }
            }
        }

        public string MainWindow_DeveloperSite
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_DeveloperSite");
                }
            }
        }

        public string MainWindow_CheckUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckUpdate");
                }
            }
        }

        public string MainWindow_VersionInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_VersionInformation");
                }
            }
        }

        public string MainWindow_SendingSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_SendingSpeed");
                }
            }
        }

        public string MainWindow_ReceivingSpeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_ReceivingSpeed");
                }
            }
        }

        public string MainWindow_DiskSpaceNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_DiskSpaceNotFound_Message");
                }
            }
        }

        public string MainWindow_CacheSpaceNotFound_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CacheSpaceNotFound_Message");
                }
            }
        }

        public string MainWindow_CheckInternalBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckInternalBlocks_Message");
                }
            }
        }

        public string MainWindow_CheckExternalBlocks_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckExternalBlocks_Message");
                }
            }
        }

        public string MainWindow_CheckBlocks_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckBlocks_State");
                }
            }
        }

        public string MainWindow_CheckUpdate_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_CheckUpdate_Message");
                }
            }
        }

        public string MainWindow_LatestVersion_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_LatestVersion_Message");
                }
            }
        }

        public string MainWindow_Close_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Close_Message");
                }
            }
        }

        public string MainWindow_Delete_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Delete_Message");
                }
            }
        }

        public string MainWindow_TransferLimit_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_TransferLimit_Message");
                }
            }
        }

        public string MainWindow_Upload_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Upload_Message");
                }
            }
        }

        public string MainWindow_Shutdown_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Shutdown_Message");
                }
            }
        }

        public string MainWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("MainWindow_Copy");
                }
            }
        }


        public string LinkOptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Title");
                }
            }
        }

        public string LinkOptionsWindow_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Download");
                }
            }
        }

        public string LinkOptionsWindow_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Upload");
                }
            }
        }

        public string LinkOptionsWindow_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Trust");
                }
            }
        }

        public string LinkOptionsWindow_Untrust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Untrust");
                }
            }
        }

        public string LinkOptionsWindow_LinkerSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_LinkerSignature");
                }
            }
        }

        public string LinkOptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Signature");
                }
            }
        }

        public string LinkOptionsWindow_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_New");
                }
            }
        }

        public string LinkOptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Edit");
                }
            }
        }

        public string LinkOptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Delete");
                }
            }
        }

        public string LinkOptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Cut");
                }
            }
        }

        public string LinkOptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Copy");
                }
            }
        }

        public string LinkOptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Paste");
                }
            }
        }

        public string LinkOptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Ok");
                }
            }
        }

        public string LinkOptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkOptionsWindow_Cancel");
                }
            }
        }


        public string OptionsWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Title");
                }
            }
        }

        public string OptionsWindow_Core
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Core");
                }
            }
        }

        public string OptionsWindow_BaseNode
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_BaseNode");
                }
            }
        }

        public string OptionsWindow_Node
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Node");
                }
            }
        }

        public string OptionsWindow_Uris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uris");
                }
            }
        }

        public string OptionsWindow_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uri");
                }
            }
        }

        public string OptionsWindow_OtherNodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_OtherNodes");
                }
            }
        }

        public string OptionsWindow_Nodes
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Nodes");
                }
            }
        }

        public string OptionsWindow_Client
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Client");
                }
            }
        }

        public string OptionsWindow_Filters
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Filters");
                }
            }
        }

        public string OptionsWindow_ConnectionType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ConnectionType");
                }
            }
        }

        public string OptionsWindow_ProxyUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ProxyUri");
                }
            }
        }

        public string OptionsWindow_UriCondition
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UriCondition");
                }
            }
        }

        public string OptionsWindow_Option
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Option");
                }
            }
        }

        public string OptionsWindow_Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Type");
                }
            }
        }

        public string OptionsWindow_Host
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Host");
                }
            }
        }

        public string OptionsWindow_Server
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Server");
                }
            }
        }

        public string OptionsWindow_ListenUris
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ListenUris");
                }
            }
        }

        public string OptionsWindow_Data
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Data");
                }
            }
        }

        public string OptionsWindow_Bandwidth
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Bandwidth");
                }
            }
        }

        public string OptionsWindow_BandwidthLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_BandwidthLimit");
                }
            }
        }

        public string OptionsWindow_Transfer
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Transfer");
                }
            }
        }

        public string OptionsWindow_TransferLimitType
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitType");
                }
            }
        }

        public string OptionsWindow_TransferLimitSpan
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitSpan");
                }
            }
        }

        public string OptionsWindow_TransferLimitSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferLimitSize");
                }
            }
        }

        public string OptionsWindow_TransferInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_TransferInformation");
                }
            }
        }

        public string OptionsWindow_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Downloaded");
                }
            }
        }

        public string OptionsWindow_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Uploaded");
                }
            }
        }

        public string OptionsWindow_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Total");
                }
            }
        }

        public string OptionsWindow_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Reset");
                }
            }
        }

        public string OptionsWindow_ConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ConnectionCount");
                }
            }
        }

        public string OptionsWindow_DownloadDirectory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_DownloadDirectory");
                }
            }
        }

        public string OptionsWindow_DownloadDirectory_Description
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_DownloadDirectory_Description");
                }
            }
        }

        public string OptionsWindow_CacheSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_CacheSize");
                }
            }
        }

        public string OptionsWindow_Events
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events");
                }
            }
        }

        public string OptionsWindow_Events_Connection
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_Connection");
                }
            }
        }

        public string OptionsWindow_Events_OpenPortAndGetIpAddress
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_OpenPortAndGetIpAddress");
                }
            }
        }

        public string OptionsWindow_Events_UseI2p
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_UseI2p");
                }
            }
        }

        public string OptionsWindow_Events_UseI2p_SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Events_UseI2p_SamBridgeUri");
                }
            }
        }

        public string OptionsWindow_Tor
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Tor");
                }
            }
        }

        public string OptionsWindow_Ipv4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ipv4");
                }
            }
        }

        public string OptionsWindow_Ipv6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ipv6");
                }
            }
        }

        public string OptionsWindow_View
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_View");
                }
            }
        }

        public string OptionsWindow_Update
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Update");
                }
            }
        }

        public string OptionsWindow_UpdateUrl
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UpdateUrl");
                }
            }
        }

        public string OptionsWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Signature");
                }
            }
        }

        public string OptionsWindow_Signatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Signatures");
                }
            }
        }

        public string OptionsWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Keywords");
                }
            }
        }

        public string OptionsWindow_Box
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Box");
                }
            }
        }

        public string OptionsWindow_RelateBoxFile
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_RelateBoxFile");
                }
            }
        }

        public string OptionsWindow_OpenBox
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_OpenBox");
                }
            }
        }

        public string OptionsWindow_ExtractTo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_ExtractTo");
                }
            }
        }

        public string OptionsWindow_UpdateOption
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_UpdateOption");
                }
            }
        }

        public string OptionsWindow_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_None");
                }
            }
        }

        public string OptionsWindow_AutoCheck
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_AutoCheck");
                }
            }
        }

        public string OptionsWindow_AutoUpdate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_AutoUpdate");
                }
            }
        }

        public string OptionsWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Value");
                }
            }
        }

        public string OptionsWindow_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Import");
                }
            }
        }

        public string OptionsWindow_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Export");
                }
            }
        }

        public string OptionsWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Up");
                }
            }
        }

        public string OptionsWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Down");
                }
            }
        }

        public string OptionsWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Add");
                }
            }
        }

        public string OptionsWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Edit");
                }
            }
        }

        public string OptionsWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Delete");
                }
            }
        }

        public string OptionsWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Ok");
                }
            }
        }

        public string OptionsWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Cancel");
                }
            }
        }

        public string OptionsWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Cut");
                }
            }
        }

        public string OptionsWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Copy");
                }
            }
        }

        public string OptionsWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_Paste");
                }
            }
        }

        public string OptionsWindow_CacheResize_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("OptionsWindow_CacheResize_Message");
                }
            }
        }


        public string ConnectionType_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_None");
                }
            }
        }

        public string ConnectionType_Tcp
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Tcp");
                }
            }
        }

        public string ConnectionType_Socks4Proxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks4Proxy");
                }
            }
        }

        public string ConnectionType_Socks4aProxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks4aProxy");
                }
            }
        }

        public string ConnectionType_Socks5Proxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_Socks5Proxy");
                }
            }
        }

        public string ConnectionType_HttpProxy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ConnectionType_HttpProxy");
                }
            }
        }


        public string TransferLimitType_None
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_None");
                }
            }
        }

        public string TransferLimitType_Downloads
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Downloads");
                }
            }
        }

        public string TransferLimitType_Uploads
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Uploads");
                }
            }
        }

        public string TransferLimitType_Total
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("TransferLimitType_Total");
                }
            }
        }


        public string ProgressWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Title");
                }
            }
        }

        public string ProgressWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Cancel");
                }
            }
        }

        public string ProgressWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ProgressWindow_Ok");
                }
            }
        }


        public string VersionInformationWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Title");
                }
            }
        }

        public string VersionInformationWindow_FileName
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_FileName");
                }
            }
        }

        public string VersionInformationWindow_Version
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Version");
                }
            }
        }

        public string VersionInformationWindow_License
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_License");
                }
            }
        }

        public string VersionInformationWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("VersionInformationWindow_Close");
                }
            }
        }


        public string SignatureWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Title");
                }
            }
        }

        public string SignatureWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Signature");
                }
            }
        }

        public string SignatureWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Ok");
                }
            }
        }

        public string SignatureWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SignatureWindow_Cancel");
                }
            }
        }


        public string NameWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Title");
                }
            }
        }

        public string NameWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Name");
                }
            }
        }

        public string NameWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Ok");
                }
            }
        }

        public string NameWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("NameWindow_Cancel");
                }
            }
        }


        public string InformationControl_Direction
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Direction");
                }
            }
        }

        public string InformationControl_Uri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Uri");
                }
            }
        }

        public string InformationControl_Priority
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Priority");
                }
            }
        }

        public string InformationControl_SentByteCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_SentByteCount");
                }
            }
        }

        public string InformationControl_ReceivedByteCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_ReceivedByteCount");
                }
            }
        }

        public string InformationControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Name");
                }
            }
        }

        public string InformationControl_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Value");
                }
            }
        }

        public string InformationControl_BufferManagerSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_BufferManagerSize");
                }
            }
        }

        public string InformationControl_CreateConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_CreateConnectionCount");
                }
            }
        }

        public string InformationControl_AcceptConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_AcceptConnectionCount");
                }
            }
        }

        public string InformationControl_BlockedConnectionCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_BlockedConnectionCount");
                }
            }
        }

        public string InformationControl_SurroundingNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_SurroundingNodeCount");
                }
            }
        }

        public string InformationControl_RelayBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_RelayBlockCount");
                }
            }
        }

        public string InformationControl_FreeSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_FreeSpace");
                }
            }
        }

        public string InformationControl_LockSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_LockSpace");
                }
            }
        }

        public string InformationControl_UsingSpace
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_UsingSpace");
                }
            }
        }

        public string InformationControl_NodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_NodeCount");
                }
            }
        }

        public string InformationControl_SeedCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_SeedCount");
                }
            }
        }

        public string InformationControl_BlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_BlockCount");
                }
            }
        }

        public string InformationControl_DownloadCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_DownloadCount");
                }
            }
        }

        public string InformationControl_UploadCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_UploadCount");
                }
            }
        }

        public string InformationControl_ShareCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_ShareCount");
                }
            }
        }

        public string InformationControl_PushNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushNodeCount");
                }
            }
        }

        public string InformationControl_PushBlockLinkCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushBlockLinkCount");
                }
            }
        }

        public string InformationControl_PushBlockRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushBlockRequestCount");
                }
            }
        }

        public string InformationControl_PushBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushBlockCount");
                }
            }
        }

        public string InformationControl_PushSeedRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushSeedRequestCount");
                }
            }
        }

        public string InformationControl_PushSeedCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PushSeedCount");
                }
            }
        }

        public string InformationControl_PullNodeCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullNodeCount");
                }
            }
        }

        public string InformationControl_PullBlockLinkCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullBlockLinkCount");
                }
            }
        }

        public string InformationControl_PullBlockRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullBlockRequestCount");
                }
            }
        }

        public string InformationControl_PullBlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullBlockCount");
                }
            }
        }

        public string InformationControl_PullSeedRequestCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullSeedRequestCount");
                }
            }
        }

        public string InformationControl_PullSeedCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_PullSeedCount");
                }
            }
        }

        public string InformationControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Copy");
                }
            }
        }

        public string InformationControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("InformationControl_Paste");
                }
            }
        }


        public string SearchControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Name");
                }
            }
        }

        public string SearchControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Signature");
                }
            }
        }

        public string SearchControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Length");
                }
            }
        }

        public string SearchControl_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Keywords");
                }
            }
        }

        public string SearchControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_CreationTime");
                }
            }
        }

        public string SearchControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Comment");
                }
            }
        }

        public string SearchControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_State");
                }
            }
        }

        public string SearchControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Id");
                }
            }
        }

        public string SearchControl_New
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_New");
                }
            }
        }

        public string SearchControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Edit");
                }
            }
        }

        public string SearchControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Delete");
                }
            }
        }

        public string SearchControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Cut");
                }
            }
        }

        public string SearchControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Copy");
                }
            }
        }

        public string SearchControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_CopyInfo");
                }
            }
        }

        public string SearchControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Paste");
                }
            }
        }

        public string SearchControl_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Export");
                }
            }
        }

        public string SearchControl_DeleteAll
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteAll");
                }
            }
        }

        public string SearchControl_DeleteCache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteCache");
                }
            }
        }

        public string SearchControl_DeleteShare
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteShare");
                }
            }
        }

        public string SearchControl_DeleteDownload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteDownload");
                }
            }
        }

        public string SearchControl_DeleteUpload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteUpload");
                }
            }
        }

        public string SearchControl_DeleteDownloadHistory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteDownloadHistory");
                }
            }
        }

        public string SearchControl_DeleteUploadHistory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_DeleteUploadHistory");
                }
            }
        }

        public string SearchControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Download");
                }
            }
        }

        public string SearchControl_Search
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Search");
                }
            }
        }

        public string SearchControl_SearchSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_SearchSignature");
                }
            }
        }

        public string SearchControl_SearchKeyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_SearchKeyword");
                }
            }
        }

        public string SearchControl_SearchCreationTimeRange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_SearchCreationTimeRange");
                }
            }
        }

        public string SearchControl_SearchState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_SearchState");
                }
            }
        }

        public string SearchControl_Filter
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Filter");
                }
            }
        }

        public string SearchControl_FilterName
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_FilterName");
                }
            }
        }

        public string SearchControl_FilterSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_FilterSignature");
                }
            }
        }

        public string SearchControl_FilterKeyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_FilterKeyword");
                }
            }
        }

        public string SearchControl_FilterCreationTimeRange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_FilterCreationTimeRange");
                }
            }
        }

        public string SearchControl_FilterSeed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_FilterSeed");
                }
            }
        }

        public string SearchControl_Information
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchControl_Information");
                }
            }
        }


        public string SearchItemEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Title");
                }
            }
        }

        public string SearchItemEditWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Name");
                }
            }
        }

        public string SearchItemEditWindow_NameRegex
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_NameRegex");
                }
            }
        }

        public string SearchItemEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Signature");
                }
            }
        }

        public string SearchItemEditWindow_Keyword
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Keyword");
                }
            }
        }

        public string SearchItemEditWindow_CreationTimeRange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_CreationTimeRange");
                }
            }
        }

        public string SearchItemEditWindow_LengthRange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_LengthRange");
                }
            }
        }

        public string SearchItemEditWindow_Seed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Seed");
                }
            }
        }

        public string SearchItemEditWindow_SearchState
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState");
                }
            }
        }

        public string SearchItemEditWindow_Contains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Contains");
                }
            }
        }

        public string SearchItemEditWindow_IsIgnoreCase
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_IsIgnoreCase");
                }
            }
        }

        public string SearchItemEditWindow_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Value");
                }
            }
        }

        public string SearchItemEditWindow_Min
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Min");
                }
            }
        }

        public string SearchItemEditWindow_Max
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Max");
                }
            }
        }

        public string SearchItemEditWindow_Up
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Up");
                }
            }
        }

        public string SearchItemEditWindow_Down
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Down");
                }
            }
        }

        public string SearchItemEditWindow_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Add");
                }
            }
        }

        public string SearchItemEditWindow_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Edit");
                }
            }
        }

        public string SearchItemEditWindow_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Delete");
                }
            }
        }

        public string SearchItemEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Ok");
                }
            }
        }

        public string SearchItemEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Cancel");
                }
            }
        }

        public string SearchItemEditWindow_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Cut");
                }
            }
        }

        public string SearchItemEditWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Copy");
                }
            }
        }

        public string SearchItemEditWindow_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_Paste");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Link
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Link");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Store
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Store");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Cache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Cache");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Downloading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Downloading");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Uploading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Uploading");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Downloaded");
                }
            }
        }

        public string SearchItemEditWindow_SearchState_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchItemEditWindow_SearchState_Uploaded");
                }
            }
        }


        public string SearchState_Link
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Link");
                }
            }
        }

        public string SearchState_Store
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Store");
                }
            }
        }

        public string SearchState_Cache
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Cache");
                }
            }
        }

        public string SearchState_Downloading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Downloading");
                }
            }
        }

        public string SearchState_Uploading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Uploading");
                }
            }
        }

        public string SearchState_Downloaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Downloaded");
                }
            }
        }

        public string SearchState_Uploaded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SearchState_Uploaded");
                }
            }
        }


        public string StoreControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreControl_Download");
                }
            }
        }

        public string StoreControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreControl_Upload");
                }
            }
        }

        public string StoreControl_Library
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreControl_Library");
                }
            }
        }


        public string DownloadControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Name");
                }
            }
        }

        public string DownloadControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Length");
                }
            }
        }

        public string DownloadControl_Priority
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority");
                }
            }
        }

        public string DownloadControl_Rank
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Rank");
                }
            }
        }

        public string DownloadControl_Rate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Rate");
                }
            }
        }

        public string DownloadControl_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Path");
                }
            }
        }

        public string DownloadControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_CreationTime");
                }
            }
        }

        public string DownloadControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_State");
                }
            }
        }

        public string DownloadControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Delete");
                }
            }
        }

        public string DownloadControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Copy");
                }
            }
        }

        public string DownloadControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_CopyInfo");
                }
            }
        }

        public string DownloadControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Paste");
                }
            }
        }

        public string DownloadControl_Priority0
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority0");
                }
            }
        }

        public string DownloadControl_Priority1
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority1");
                }
            }
        }

        public string DownloadControl_Priority2
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority2");
                }
            }
        }

        public string DownloadControl_Priority3
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority3");
                }
            }
        }

        public string DownloadControl_Priority4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority4");
                }
            }
        }

        public string DownloadControl_Priority5
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority5");
                }
            }
        }

        public string DownloadControl_Priority6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Priority6");
                }
            }
        }

        public string DownloadControl_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_Reset");
                }
            }
        }

        public string DownloadControl_DeleteComplete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadControl_DeleteComplete");
                }
            }
        }


        public string DownloadState_Downloading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadState_Downloading");
                }
            }
        }

        public string DownloadState_Decoding
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadState_Decoding");
                }
            }
        }

        public string DownloadState_ParityDecoding
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadState_ParityDecoding");
                }
            }
        }

        public string DownloadState_Completed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadState_Completed");
                }
            }
        }

        public string DownloadState_Error
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("DownloadState_Error");
                }
            }
        }


        public string UploadControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Name");
                }
            }
        }

        public string UploadControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Length");
                }
            }
        }

        public string UploadControl_Priority
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority");
                }
            }
        }

        public string UploadControl_Rank
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Rank");
                }
            }
        }

        public string UploadControl_Rate
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Rate");
                }
            }
        }

        public string UploadControl_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Path");
                }
            }
        }

        public string UploadControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_CreationTime");
                }
            }
        }

        public string UploadControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_State");
                }
            }
        }

        public string UploadControl_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Add");
                }
            }
        }

        public string UploadControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Delete");
                }
            }
        }

        public string UploadControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Copy");
                }
            }
        }

        public string UploadControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_CopyInfo");
                }
            }
        }

        public string UploadControl_Priority0
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority0");
                }
            }
        }

        public string UploadControl_Priority1
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority1");
                }
            }
        }

        public string UploadControl_Priority2
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority2");
                }
            }
        }

        public string UploadControl_Priority3
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority3");
                }
            }
        }

        public string UploadControl_Priority4
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority4");
                }
            }
        }

        public string UploadControl_Priority5
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority5");
                }
            }
        }

        public string UploadControl_Priority6
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Priority6");
                }
            }
        }

        public string UploadControl_Reset
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_Reset");
                }
            }
        }

        public string UploadControl_DeleteComplete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadControl_DeleteComplete");
                }
            }
        }


        public string UploadState_ComputeHash
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_ComputeHash");
                }
            }
        }

        public string UploadState_Encoding
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_Encoding");
                }
            }
        }

        public string UploadState_ParityEncoding
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_ParityEncoding");
                }
            }
        }

        public string UploadState_Uploading
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_Uploading");
                }
            }
        }

        public string UploadState_Completed
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_Completed");
                }
            }
        }

        public string UploadState_Error
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadState_Error");
                }
            }
        }


        public string ShareControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_Name");
                }
            }
        }

        public string ShareControl_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_Path");
                }
            }
        }

        public string ShareControl_BlockCount
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_BlockCount");
                }
            }
        }

        public string ShareControl_Add
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_Add");
                }
            }
        }

        public string ShareControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_Delete");
                }
            }
        }

        public string ShareControl_CheckExist
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("ShareControl_CheckExist");
                }
            }
        }


        public string UploadWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Title");
                }
            }
        }

        public string UploadWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Name");
                }
            }
        }

        public string UploadWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Keywords");
                }
            }
        }

        public string UploadWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Signature");
                }
            }
        }

        public string UploadWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Comment");
                }
            }
        }

        public string UploadWindow_List
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_List");
                }
            }
        }

        public string UploadWindow_Path
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Path");
                }
            }
        }

        public string UploadWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Ok");
                }
            }
        }

        public string UploadWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("UploadWindow_Cancel");
                }
            }
        }


        public string LinkControl_Trust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Trust");
                }
            }
        }

        public string LinkControl_Untrust
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Untrust");
                }
            }
        }

        public string LinkControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Signature");
                }
            }
        }

        public string LinkControl_Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Value");
                }
            }
        }

        public string LinkControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Delete");
                }
            }
        }

        public string LinkControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Cut");
                }
            }
        }

        public string LinkControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Copy");
                }
            }
        }

        public string LinkControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LinkControl_Paste");
                }
            }
        }


        public string StoreDownloadControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Name");
                }
            }
        }

        public string StoreDownloadControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Signature");
                }
            }
        }

        public string StoreDownloadControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Length");
                }
            }
        }

        public string StoreDownloadControl_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Keywords");
                }
            }
        }

        public string StoreDownloadControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_CreationTime");
                }
            }
        }

        public string StoreDownloadControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Comment");
                }
            }
        }

        public string StoreDownloadControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_State");
                }
            }
        }

        public string StoreDownloadControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Id");
                }
            }
        }

        public string StoreDownloadControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Upload");
                }
            }
        }

        public string StoreDownloadControl_NewStore
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_NewStore");
                }
            }
        }

        public string StoreDownloadControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_NewCategory");
                }
            }
        }

        public string StoreDownloadControl_NewBox
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_NewBox");
                }
            }
        }

        public string StoreDownloadControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Edit");
                }
            }
        }

        public string StoreDownloadControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Delete");
                }
            }
        }

        public string StoreDownloadControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Cut");
                }
            }
        }

        public string StoreDownloadControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Copy");
                }
            }
        }

        public string StoreDownloadControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Paste");
                }
            }
        }

        public string StoreDownloadControl_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Import");
                }
            }
        }

        public string StoreDownloadControl_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Export");
                }
            }
        }

        public string StoreDownloadControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_CopyInfo");
                }
            }
        }

        public string StoreDownloadControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_Download");
                }
            }
        }

        public string StoreDownloadControl_DigitalSignatureAnnulled_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_DigitalSignatureAnnulled_Message");
                }
            }
        }

        public string StoreDownloadControl_DigitalSignatureError_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreDownloadControl_DigitalSignatureError_Message");
                }
            }
        }


        public string StoreUploadControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Name");
                }
            }
        }

        public string StoreUploadControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Signature");
                }
            }
        }

        public string StoreUploadControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Length");
                }
            }
        }

        public string StoreUploadControl_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Keywords");
                }
            }
        }

        public string StoreUploadControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_CreationTime");
                }
            }
        }

        public string StoreUploadControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Comment");
                }
            }
        }

        public string StoreUploadControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_State");
                }
            }
        }

        public string StoreUploadControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Id");
                }
            }
        }

        public string StoreUploadControl_Upload
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Upload");
                }
            }
        }

        public string StoreUploadControl_NewStore
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_NewStore");
                }
            }
        }

        public string StoreUploadControl_NewCategory
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_NewCategory");
                }
            }
        }

        public string StoreUploadControl_NewBox
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_NewBox");
                }
            }
        }

        public string StoreUploadControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Edit");
                }
            }
        }

        public string StoreUploadControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Delete");
                }
            }
        }

        public string StoreUploadControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Cut");
                }
            }
        }

        public string StoreUploadControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Copy");
                }
            }
        }

        public string StoreUploadControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Paste");
                }
            }
        }

        public string StoreUploadControl_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Import");
                }
            }
        }

        public string StoreUploadControl_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Export");
                }
            }
        }

        public string StoreUploadControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_CopyInfo");
                }
            }
        }

        public string StoreUploadControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_Download");
                }
            }
        }

        public string StoreUploadControl_DigitalSignatureAnnulled_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_DigitalSignatureAnnulled_Message");
                }
            }
        }

        public string StoreUploadControl_DigitalSignatureError_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("StoreUploadControl_DigitalSignatureError_Message");
                }
            }
        }


        public string LibraryControl_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Name");
                }
            }
        }

        public string LibraryControl_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Signature");
                }
            }
        }

        public string LibraryControl_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Length");
                }
            }
        }

        public string LibraryControl_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Keywords");
                }
            }
        }

        public string LibraryControl_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_CreationTime");
                }
            }
        }

        public string LibraryControl_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Comment");
                }
            }
        }

        public string LibraryControl_State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_State");
                }
            }
        }

        public string LibraryControl_Id
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Id");
                }
            }
        }

        public string LibraryControl_NewBox
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_NewBox");
                }
            }
        }

        public string LibraryControl_Edit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Edit");
                }
            }
        }

        public string LibraryControl_Delete
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Delete");
                }
            }
        }

        public string LibraryControl_Cut
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Cut");
                }
            }
        }

        public string LibraryControl_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Copy");
                }
            }
        }

        public string LibraryControl_Paste
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Paste");
                }
            }
        }

        public string LibraryControl_Import
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Import");
                }
            }
        }

        public string LibraryControl_Export
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Export");
                }
            }
        }

        public string LibraryControl_CopyInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_CopyInfo");
                }
            }
        }

        public string LibraryControl_Download
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_Download");
                }
            }
        }

        public string LibraryControl_DigitalSignatureAnnulled_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_DigitalSignatureAnnulled_Message");
                }
            }
        }

        public string LibraryControl_DigitalSignatureError_Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("LibraryControl_DigitalSignatureError_Message");
                }
            }
        }


        public string SeedEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Title");
                }
            }
        }

        public string SeedEditWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Name");
                }
            }
        }

        public string SeedEditWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Keywords");
                }
            }
        }

        public string SeedEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Signature");
                }
            }
        }

        public string SeedEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Comment");
                }
            }
        }

        public string SeedEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Ok");
                }
            }
        }

        public string SeedEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedEditWindow_Cancel");
                }
            }
        }


        public string BoxEditWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Title");
                }
            }
        }

        public string BoxEditWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Name");
                }
            }
        }

        public string BoxEditWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Signature");
                }
            }
        }

        public string BoxEditWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Comment");
                }
            }
        }

        public string BoxEditWindow_Ok
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Ok");
                }
            }
        }

        public string BoxEditWindow_Cancel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("BoxEditWindow_Cancel");
                }
            }
        }


        public string SeedInformationWindow_Title
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Title");
                }
            }
        }

        public string SeedInformationWindow_Property
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Property");
                }
            }
        }

        public string SeedInformationWindow_Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Name");
                }
            }
        }

        public string SeedInformationWindow_Keywords
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Keywords");
                }
            }
        }

        public string SeedInformationWindow_Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Signature");
                }
            }
        }

        public string SeedInformationWindow_CreationTime
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_CreationTime");
                }
            }
        }

        public string SeedInformationWindow_Length
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Length");
                }
            }
        }

        public string SeedInformationWindow_Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Comment");
                }
            }
        }

        public string SeedInformationWindow_Store
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Store");
                }
            }
        }

        public string SeedInformationWindow_Copy
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Copy");
                }
            }
        }

        public string SeedInformationWindow_Close
        {
            get
            {
                lock (this.ThisLock)
                {
                    return this.Translate("SeedInformationWindow_Close");
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

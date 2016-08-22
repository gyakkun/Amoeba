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
                new Library.Configuration.SettingContent<LockedList<string>>() { Name = "Global_SearchKeywords", Value = new LockedList<string>() },
                new Library.Configuration.SettingContent<string>() { Name = "Global_UseLanguage", Value = "English" },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConnectRunning", Value = true },
                new Library.Configuration.SettingContent<bool>() { Name = "Global_IsConvertRunning", Value = true },

                new Library.Configuration.SettingContent<StoreCategorizeTreeItem>() { Name = "Store_Download_StoreCategorizeTreeItem", Value = new StoreCategorizeTreeItem() { Name = "Category" } },
                new Library.Configuration.SettingContent<StoreCategorizeTreeItem>() { Name = "Store_Upload_StoreCategorizeTreeItem", Value = new StoreCategorizeTreeItem() { Name = "Category" } },
                new Library.Configuration.SettingContent<Box>() { Name = "Store_Library_Box", Value = new Box() { Name = "Library" } },

                new Library.Configuration.SettingContent<LockedHashDictionary<string, LinkItem>>() { Name = "Cache_LinkItems", Value = new LockedHashDictionary<string, LinkItem>() },
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


        public StoreCategorizeTreeItem Store_Download_StoreCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (StoreCategorizeTreeItem)this["Store_Download_StoreCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Store_Download_StoreCategorizeTreeItem"] = value;
                }
            }
        }

        public StoreCategorizeTreeItem Store_Upload_StoreCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (StoreCategorizeTreeItem)this["Store_Upload_StoreCategorizeTreeItem"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Store_Upload_StoreCategorizeTreeItem"] = value;
                }
            }
        }

        public Box Store_Library_Box
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (Box)this["Store_Library_Box"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Store_Library_Box"] = value;
                }
            }
        }


        public LockedHashDictionary<string, LinkItem> Cache_LinkItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    return (LockedHashDictionary<string, LinkItem>)this["Cache_LinkItems"];
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    this["Cache_LinkItems"] = value;
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

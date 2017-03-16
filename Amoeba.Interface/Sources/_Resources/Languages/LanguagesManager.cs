using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using System.Xml;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Amoeba.Interface
{
    delegate void UsingLanguageChangedEventHandler(object sender);

    partial class LanguagesManager
    {
        private static readonly LanguagesManager _defaultInstance = new LanguagesManager();
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();
        private static string _currentLanguage;

        public static UsingLanguageChangedEventHandler UsingLanguageChangedEvent;

        protected static void OnUsingLanguageChangedEvent()
        {
            LanguagesManager.UsingLanguageChangedEvent?.Invoke(_defaultInstance);
        }

        static LanguagesManager()
        {
#if DEBUG
            string path = @"C:\Local\Projects\Alliance-Network\Amoeba\Amoeba.Interface\Languages";

            if (!Directory.Exists(path)) path = EnvironmentConfig.Paths.LanguagesPath;
#else
            string path = EnvironmentConfig.Paths.LanguagesPath;
#endif

            LanguagesManager.Load(path);
        }

        private static void Load(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            _dic.Clear();

            foreach (string path in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var dic = new Dictionary<string, string>();

                    using (var xml = new XmlTextReader(path))
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

                    _dic[Path.GetFileNameWithoutExtension(path)] = dic;
                }
                catch (XmlException)
                {

                }
            }

            if (CultureInfo.CurrentUICulture.Name == "ja-JP" && _dic.Keys.Contains("Japanese"))
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

        public static void ChangeLanguage(string language)
        {
            if (!_dic.ContainsKey(language)) throw new ArgumentException();

            _currentLanguage = language;

            if (System.Windows.Application.Current != null)
            {
                var provider = (ObjectDataProvider)System.Windows.Application.Current.FindResource("_languages");
                provider.Refresh();
            }

            LanguagesManager.OnUsingLanguageChangedEvent();
        }

        public IEnumerable<string> Languages
        {
            get
            {
                var dic = new Dictionary<string, string>();

                foreach (string path in _dic.Keys.ToList())
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

            if (_dic[_currentLanguage].TryGetValue(key, out string result))
            {
                return Regex.Unescape(result);
            }

            return null;
        }
    }
}

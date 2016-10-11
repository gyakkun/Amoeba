using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Library;

namespace Amoeba
{
    class Config
    {
        public StartupSetting Startup { get; private set; }
        public CatharsisSetting Catharsis { get; private set; }
        public CacheSetting Cache { get; private set; }
        public ColorsSetting Colors { get; private set; }

        public void Load(string directoryPath)
        {
            // Init
            {
                {
                    var startupSetting = new StartupSetting();

                    startupSetting.ProcessSettings.Add(new ProcessSetting()
                    {
                        Path = @"Assemblies/Tor/tor.exe",
                        Arguments = "-f torrc DataDirectory " + @"../../../Work/Tor",
                        WorkingDirectory = @"Assemblies/Tor",
                    });

                    startupSetting.ProcessSettings.Add(new ProcessSetting()
                    {
                        Path = @"Assemblies/Polipo/polipo.exe",
                        Arguments = "-c polipo.conf",
                        WorkingDirectory = @"Assemblies/Polipo",
                    });

                    this.Startup = startupSetting;
                }

                {
                    var catharsisSettings = new CatharsisSetting();
                    catharsisSettings.Ipv4AddressFilters.Add(new Ipv4AddressFilter("tcp:127.0.0.1:18118", null, new string[] { @"Catharsis_Ipv4.txt" }));

                    this.Catharsis = catharsisSettings;
                }

                {
                    var cacheSettings = new CacheSetting();
                    cacheSettings.Path = Path.Combine(directoryPath, "Cache.blocks");

                    this.Cache = cacheSettings;
                }

                {
                    var colorsSetting = new ColorsSetting();
                    colorsSetting.Tree_Hit = System.Windows.Media.Colors.LightPink;
                    colorsSetting.Link = System.Windows.Media.Colors.SkyBlue;
                    colorsSetting.Link_New = System.Windows.Media.Colors.LightPink;
                    colorsSetting.Message_Trust = System.Windows.Media.Colors.SkyBlue;
                    colorsSetting.Message_Untrust = System.Windows.Media.Colors.LightPink;

                    this.Colors = colorsSetting;
                }
            }

            // Config
            {
                var startupConfigPath = Path.Combine(directoryPath, "Startup.config");
                var catharsisConfigPath = Path.Combine(directoryPath, "Catharsis.config");
                var cacheConfigPath = Path.Combine(directoryPath, "Cache.config");
                var colorsConfigPath = Path.Combine(directoryPath, "Colors.config");

                if (!File.Exists(startupConfigPath))
                {
                    this.SaveObject(startupConfigPath, this.Startup);
                }

                if (!File.Exists(catharsisConfigPath))
                {
                    this.SaveObject(catharsisConfigPath, this.Catharsis);
                }

                if (!File.Exists(cacheConfigPath))
                {
                    this.SaveObject(cacheConfigPath, this.Cache);
                }

                if (!File.Exists(colorsConfigPath))
                {
                    this.SaveObject(colorsConfigPath, this.Colors);
                }

                this.Startup = this.LoadObject<StartupSetting>(startupConfigPath);
                this.Catharsis = this.LoadObject<CatharsisSetting>(catharsisConfigPath);
                this.Cache = this.LoadObject<CacheSetting>(cacheConfigPath);
                this.Colors = this.LoadObject<ColorsSetting>(colorsConfigPath);
            }
        }

        private T LoadObject<T>(string configPath)
        {
            try
            {
                using (var stream = new FileStream(configPath, FileMode.Open))
                {
                    return JsonUtils.Load<T>(stream);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }

            return default(T);
        }

        private void SaveObject(string configPath, object value)
        {
            try
            {
                using (var stream = new FileStream(configPath, FileMode.Create))
                {
                    JsonUtils.Save(stream, value, true);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }
        }

        [DataContract(Name = "StartupSetting")]
        public class StartupSetting
        {
            private List<ProcessSetting> _processSettings;

            [DataMember(Name = "ProcessSettings")]
            public List<ProcessSetting> ProcessSettings
            {
                get
                {
                    if (_processSettings == null)
                        _processSettings = new List<ProcessSetting>();

                    return _processSettings;
                }
            }
        }

        [DataContract(Name = "ProcessSetting")]
        public class ProcessSetting
        {
            [DataMember(Name = "Path")]
            public string Path { get; set; }

            [DataMember(Name = "Arguments")]
            public string Arguments { get; set; }

            [DataMember(Name = "WorkingDirectory")]
            public string WorkingDirectory { get; set; }
        }

        [DataContract(Name = "CatharsisSetting")]
        public class CatharsisSetting
        {
            private List<Ipv4AddressFilter> _ipv4AddressFilters;

            [DataMember(Name = "Ipv4AddressFilters")]
            public List<Ipv4AddressFilter> Ipv4AddressFilters
            {
                get
                {
                    if (_ipv4AddressFilters == null)
                        _ipv4AddressFilters = new List<Ipv4AddressFilter>();

                    return _ipv4AddressFilters;
                }
            }
        }

        [DataContract(Name = "CacheSetting")]
        public class CacheSetting
        {
            [DataMember(Name = "Path")]
            public string Path { get; set; }
        }

        [DataContract(Name = "ColorsSetting")]
        public class ColorsSetting
        {
            [DataMember(Name = "Tree_Hit")]
            public System.Windows.Media.Color Tree_Hit { get; set; }

            [DataMember(Name = "Link")]
            public System.Windows.Media.Color Link { get; set; }

            [DataMember(Name = "Link_New")]
            public System.Windows.Media.Color Link_New { get; set; }

            [DataMember(Name = "Message_Trust")]
            public System.Windows.Media.Color Message_Trust { get; set; }

            [DataMember(Name = "Message_Untrust")]
            public System.Windows.Media.Color Message_Untrust { get; set; }
        }
    }
}

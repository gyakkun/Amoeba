using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Omnius.Configuration;

namespace Amoeba.Interface
{
    partial class SettingsManager
    {
        private static readonly SettingsManager _defaultInstance = new SettingsManager(EnvironmentConfig.Paths.SettingsPath);

        static SettingsManager()
        {

        }

        private void Init()
        {

        }

        public static SettingsManager Instance
        {
            get
            {
                return _defaultInstance;
            }
        }
    }
}

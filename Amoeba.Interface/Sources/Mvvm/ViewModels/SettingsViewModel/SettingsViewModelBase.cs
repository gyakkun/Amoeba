using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Configuration;

namespace Amoeba.Interface
{
    abstract class SettingsViewModelBase : DynamicViewModelBase, ISettings, IDisposable
    {
        public abstract void Load();
        public abstract void Save();

        public abstract void Dispose();
    }
}

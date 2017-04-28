using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Omnius.Configuration;

namespace Amoeba.Interface
{
    partial class SettingsManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static SettingsManager Instance { get; } = new SettingsManager(System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "Settings"));

        private void Init()
        {

        }
    }
}

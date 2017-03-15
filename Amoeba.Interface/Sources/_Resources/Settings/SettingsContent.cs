using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(SettingsContent<T>))]
    class SettingsContent<T> : INotifyPropertyChanged, ISynchronized
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private T _value;

        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!_value.Equals(value))
                {
                    _value = value;
                    this.OnPropertyChanged(nameof(Value));
                }
            }
        }

        private readonly object _lockObject = new object();

        public object LockObject
        {
            get
            {
                return _lockObject;
            }
        }
    }
}

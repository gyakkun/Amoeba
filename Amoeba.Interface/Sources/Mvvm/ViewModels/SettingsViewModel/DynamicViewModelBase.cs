using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;

namespace Amoeba.Interface
{
    // http://blog.okazuki.jp/entry/20100702/1278056325
    abstract class DynamicViewModelBase : DynamicObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Dictionary<string, object> _pairs = new Dictionary<string, object>();

        public void SetPairs(IEnumerable<KeyValuePair<string, object>> pairs)
        {
            _pairs.Clear();

            foreach (var (key, value) in pairs)
            {
                _pairs[key] = value;
            }
        }

        public IEnumerable<KeyValuePair<string, object>> GetPairs()
        {
            return _pairs.ToArray();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_pairs.TryGetValue(binder.Name, out var value))
            {
                result = value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _pairs[binder.Name] = value;
            this.OnPropertyChanged(binder.Name);

            return true;
        }
    }
}

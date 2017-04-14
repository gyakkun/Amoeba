using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba
{
    [DataContract(Name = nameof(I2pConnectionConfig))]
    public class I2pConnectionConfig
    {
        private bool _isEnabled;
        private string _samBridgeUri;

        private I2pConnectionConfig() { }

        public I2pConnectionConfig(bool isEnabled, string samBridgeUri)
        {
            this.IsEnabled = isEnabled;
            this.SamBridgeUri = samBridgeUri;
        }

        [DataMember(Name = nameof(IsEnabled))]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            private set
            {
                _isEnabled = value;
            }
        }

        [DataMember(Name = nameof(SamBridgeUri))]
        public string SamBridgeUri
        {
            get
            {
                return _samBridgeUri;
            }
            private set
            {
                _samBridgeUri = value;
            }
        }
    }
}

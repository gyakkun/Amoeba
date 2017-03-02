using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba
{
    [DataContract(Name = "ConnectionI2pConfig")]
    public class ConnectionI2pConfig
    {
        private bool _isEnabled;
        private string _samBridgeUri;

        public ConnectionI2pConfig(bool isEnabled, string samBridgeUri)
        {
            this.IsEnabled = isEnabled;
            this.SamBridgeUri = samBridgeUri;
        }

        [DataMember(Name = "IsEnabled")]
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

        [DataMember(Name = "SamBridgeUri")]
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

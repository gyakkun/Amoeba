using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Omnius.Configuration;
using System.Collections.ObjectModel;
using Omnius.Security;

namespace Amoeba.Interface
{
    partial class SettingsManager
    {
        private DigitalSignature _digitalSignature;

        [DataMember(Name = nameof(DigitalSignature))]
        public DigitalSignature DigitalSignature
        {
            get
            {
                return _digitalSignature;
            }
            set
            {
                if (_digitalSignature != value)
                {
                    _digitalSignature = value;
                    this.OnPropertyChanged(nameof(DigitalSignature));
                }
            }
        }

        private ObservableCollection<PublishDirectoryInfo> _publishDirectoryInfos;

        [DataMember(Name = nameof(PublishDirectoryInfos))]
        public ObservableCollection<PublishDirectoryInfo> PublishDirectoryInfos
        {
            get
            {
                if (_publishDirectoryInfos == null)
                    _publishDirectoryInfos = new ObservableCollection<PublishDirectoryInfo>();

                return _publishDirectoryInfos;
            }
        }

    }
}

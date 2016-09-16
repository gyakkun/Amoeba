using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Amoeba;
using Library.Security;
using Library.Io;
using Library.Collections;

namespace Amoeba.Windows
{
    [DataContract(Name = "ProfileSetting")]
    class ProfileSetting : ICloneable<ProfileSetting>, IThisLock
    {
        private Exchange _exchange;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        [DataMember(Name = "Exchange")]
        public Exchange Exchange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _exchange;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _exchange = value;
                }
            }
        }

        #region ICloneable<ProfileSetting>

        public ProfileSetting Clone()
        {
            lock (this.ThisLock)
            {
                return JsonUtils.Clone(this);
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }
}

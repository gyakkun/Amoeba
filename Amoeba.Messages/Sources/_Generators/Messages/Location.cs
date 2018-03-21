using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Messages
{
    public sealed partial class Location
    {
        public override string ToString()
        {
            return string.Join(", ", this.Uris);
        }
    }
}

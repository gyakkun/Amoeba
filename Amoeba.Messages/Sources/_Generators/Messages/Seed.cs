using System;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Messages
{
    public sealed partial class Seed
    {
        public override string ToString()
        {
            return this.Name;
        }
    }
}

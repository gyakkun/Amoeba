using Amoeba.Service;
using Omnius.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Amoeba.Interface
{
    class SearchItemViewModel
    {
        public BitmapSource Icon { get; set; }
        public string Name { get; set; }
        public Signature Signature { get; set; }
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public SearchState State { get; set; }
        public Seed Model { get; set; }
    }
}

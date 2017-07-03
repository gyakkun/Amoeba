using System;
using System.Windows.Media.Imaging;
using Amoeba.Service;
using Omnius.Security;

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

using System;
using System.Windows.Media.Imaging;

namespace Amoeba.Interface
{
    class SubscribeItemViewModel
    {
        public BitmapSource Icon { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public object Model { get; set; }
    }
}

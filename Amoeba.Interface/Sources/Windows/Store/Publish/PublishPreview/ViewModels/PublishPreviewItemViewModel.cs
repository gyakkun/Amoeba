using System.Windows.Media.Imaging;

namespace Amoeba.Interface
{
    class PublishPreviewItemViewModel
    {
        public BitmapSource Icon { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
    }
}

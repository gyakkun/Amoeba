using System;

namespace Amoeba.Interface
{
    partial class UploadCategoryInfo
    {
        public DateTime CreationTime
        {
            get
            {
                var maxTime = DateTime.MinValue;

                foreach (var categoryInfo in this.CategoryInfos)
                {
                    var time = categoryInfo.CreationTime;
                    if (maxTime < time) maxTime = time;
                }

                foreach (var directoryInfo in this.DirectoryInfos)
                {
                    var time = directoryInfo.CreationTime;
                    if (maxTime < time) maxTime = time;
                }

                return maxTime;
            }
        }

        public long Length
        {
            get
            {
                long totalLength = 0;

                foreach (var categoryInfo in this.CategoryInfos)
                {
                    totalLength += categoryInfo.Length;
                }

                foreach (var directoryInfo in this.DirectoryInfos)
                {
                    totalLength += directoryInfo.Length;
                }

                return totalLength;
            }
        }
    }
}

using System;

namespace Amoeba.Interface
{
    partial class UploadBoxInfo
    {
        public DateTime CreationTime
        {
            get
            {
                var maxTime = DateTime.MinValue;

                foreach (var boxInfo in this.BoxInfos)
                {
                    var time = boxInfo.CreationTime;
                    if (maxTime < time) maxTime = time;
                }

                foreach (var seed in this.Seeds)
                {
                    var time = seed.CreationTime;
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

                foreach (var boxInfo in this.BoxInfos)
                {
                    totalLength += boxInfo.Length;
                }

                foreach (var seed in this.Seeds)
                {
                    totalLength += seed.Length;
                }

                return totalLength;
            }
        }
    }
}

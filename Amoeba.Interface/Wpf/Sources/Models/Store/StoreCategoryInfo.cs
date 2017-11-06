using System;

namespace Amoeba.Interface
{
    partial class StoreCategoryInfo
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

                foreach (var signatureInfo in this.SignatureInfos)
                {
                    var time = signatureInfo.CreationTime;
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

                foreach (var signatureInfo in this.SignatureInfos)
                {
                    totalLength += signatureInfo.Length;
                }

                return totalLength;
            }
        }
    }
}

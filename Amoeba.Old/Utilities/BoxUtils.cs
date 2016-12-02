using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Library.Net.Amoeba;

namespace Amoeba
{
    static class BoxUtils
    {
        public static long GetLength(Box box)
        {
            long length = 0;

            foreach (var item in box.Seeds)
            {
                length += item.Length;
            }

            foreach (var item in box.Boxes)
            {
                length += GetLength(item);
            }

            return length;
        }

        public static DateTime GetCreationTime(Box box)
        {
            var seedList = new List<Seed>();
            {
                var boxList = new List<Box>();
                boxList.Add(box);

                for (int i = 0; i < boxList.Count; i++)
                {
                    boxList.AddRange(boxList[i].Boxes);
                    seedList.AddRange(boxList[i].Seeds);
                }
            }

            if (seedList.Count == 0) return DateTime.MinValue;
            else return seedList.Max(n => n.CreationTime);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class SubscribeItemViewModel
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public object Model { get; set; }
    }
}

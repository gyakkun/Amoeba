using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    interface IDropable
    {
        bool TryAdd(object value);
        bool TryRemove(object value);
    }
}

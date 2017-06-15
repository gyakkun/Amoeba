using Omnius.Base;
using Omnius.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class Backup
    {
        public event Action SaveEvent;

        public static Backup Instance { get; } = new Backup();

        private Backup()
        {

        }

        public void Run()
        {
            this.SaveEvent?.Invoke();
        }
    }
}

using System;

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

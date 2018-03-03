using System;

namespace Amoeba.Interface
{
    class EventHooks
    {
        public event Action SaveEvent;

        public static EventHooks Instance { get; } = new EventHooks();

        private EventHooks() { }

        public void OnSave()
        {
            this.SaveEvent?.Invoke();
        }
    }
}

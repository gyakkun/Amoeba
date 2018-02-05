using System;

namespace Amoeba.Interface
{
    class NoticeWindowViewModel
    {
        public event Action Callback;

        public NoticeWindowViewModel(string message)
        {
            this.Message = message;
        }

        public string Message { get; private set; }

        public void Ok()
        {
            this.Callback?.Invoke();
        }
    }
}

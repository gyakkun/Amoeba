using System;

namespace Amoeba.Interface
{
    public enum ConfirmWindowType
    {
        Delete,
    }

    class ConfirmWindowViewModel
    {
        public event Action Callback;

        public ConfirmWindowViewModel(ConfirmWindowType type)
        {
            if (type == ConfirmWindowType.Delete)
            {
                this.Message = LanguagesManager.Instance.ConfirmWindow_DeleteMessage;
            }
        }

        public ConfirmWindowViewModel(string message)
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

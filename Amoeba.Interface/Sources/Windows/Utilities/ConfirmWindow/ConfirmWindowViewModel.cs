using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Omnius.Security;

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

        public string Message { get; private set; }

        public void Ok()
        {
            this.Callback?.Invoke();
        }
    }
}

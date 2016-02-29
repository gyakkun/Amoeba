using System.Windows.Controls;
using Amoeba.Properties;

namespace Amoeba.Windows
{
    class LanguageMenuItem : MenuItem
    {
        private string _value;

        public LanguageMenuItem()
        {
            LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
        }

        void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            this.Update();
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }

        private void Update()
        {
            base.Header = LanguagesManager.Instance.Translate("Languages_" + _value) ?? _value;
        }
    }
}

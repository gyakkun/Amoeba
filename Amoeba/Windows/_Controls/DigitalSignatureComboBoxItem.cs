using System.Windows.Controls;
using Library.Security;

namespace Amoeba.Windows
{
    class DigitalSignatureComboBoxItem : ComboBoxItem
    {
        private DigitalSignature _value;

        public DigitalSignatureComboBoxItem()
        {

        }

        public DigitalSignatureComboBoxItem(DigitalSignature digitalSignature)
        {
            this.Value = digitalSignature;
        }

        public void Update()
        {
            this.Content = this.Value.ToString();
        }

        public DigitalSignature Value
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
    }
}

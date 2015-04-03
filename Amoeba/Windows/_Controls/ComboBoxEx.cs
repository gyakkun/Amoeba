using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Amoeba.Windows
{
    class ComboBoxEx : ComboBox
    {
        [Bindable(true, BindingDirection.OneWay)]
        public int MaxLength
        {
            get
            {
                return (int)GetValue(MaxLengthProperty);
            }
            set
            {
                SetValue(MaxLengthProperty, value);
            }
        }

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register("MaxLength", typeof(int),
            typeof(ComboBoxEx),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.None,
            new PropertyChangedCallback(OnMaxLengthChanged)));

        private static void SetMaxLength(ComboBoxEx comboBoxEx)
        {
            if (comboBoxEx != null && comboBoxEx.Template != null)
            {
                var textBox = comboBoxEx.Template.FindName("PART_EditableTextBox", comboBoxEx) as TextBox;

                if (textBox != null)
                {
                    textBox.MaxLength = comboBoxEx.MaxLength;
                }
            }
        }

        private static void OnMaxLengthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            SetMaxLength(obj as ComboBoxEx);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Loaded += (object sender, RoutedEventArgs args) =>
            {
                SetMaxLength(this);
            };
        }
    }
}

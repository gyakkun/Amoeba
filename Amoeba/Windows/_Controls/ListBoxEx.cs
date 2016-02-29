using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amoeba.Windows
{
    class ListBoxEx : ListBox
    {
        public new void SetSelectedItems(IEnumerable selectedItems)
        {
            base.SetSelectedItems(selectedItems);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            if (e.OriginalSource is ContentControl)
            {
                if (this.GetCurrentIndex(e.GetPosition) == -1)
                {
                    try
                    {
                        this.UnselectAll();
                    }
                    catch (Exception)
                    {

                    }

                    base.Focus();
                }
            }
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);

            if (e.OriginalSource is ContentControl)
            {
                if (this.GetCurrentIndex(e.GetPosition) == -1)
                {
                    try
                    {
                        this.UnselectAll();
                    }
                    catch (Exception)
                    {

                    }

                    base.Focus();
                }
            }
        }
    }
}
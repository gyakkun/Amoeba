using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amoeba.Windows
{
    class ListViewEx : ListView
    {
        public new void SetSelectedItems(IEnumerable selectedItems)
        {
            base.SetSelectedItems(selectedItems);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            Point lposition = e.GetPosition(this);

            if ((this.ActualWidth - lposition.X) < 15
                || (this.ActualHeight - lposition.Y) < 15)
            {
                return;
            }

            var posithonIndex = this.GetCurrentIndex(e.GetPosition);

            if (posithonIndex == -1 || lposition.Y < 25)
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

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonDown(e);

            Point lposition = e.GetPosition(this);

            if ((this.ActualWidth - lposition.X) < 15
                || (this.ActualHeight - lposition.Y) < 15)
            {
                return;
            }

            var posithonIndex = this.GetCurrentIndex(e.GetPosition);

            if (posithonIndex == -1 || lposition.Y < 25)
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
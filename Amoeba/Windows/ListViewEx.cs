using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Amoeba.Properties;

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
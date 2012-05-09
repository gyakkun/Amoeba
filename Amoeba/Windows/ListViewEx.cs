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
        public void SetSelectedItems(IEnumerable selectedItems)
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
                if (this.SelectionMode != System.Windows.Controls.SelectionMode.Single)
                {
                    try
                    {
                        this.SelectedItems.Clear();
                    }
                    catch (Exception)
                    {

                    }
                }

                try
                {
                    this.SelectedItem = null;
                }
                catch (Exception)
                {

                }

                try
                {
                    this.SelectedIndex = -1;
                }
                catch (Exception)
                {

                }
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
                if (this.SelectionMode != System.Windows.Controls.SelectionMode.Single)
                {
                    try
                    {
                        this.SelectedItems.Clear();
                    }
                    catch (Exception)
                    {

                    }
                }

                try
                {
                    this.SelectedItem = null;
                }
                catch (Exception)
                {

                }

                try
                {
                    this.SelectedIndex = -1;
                }
                catch (Exception)
                {

                }
            }
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseRightButtonUp(e);

            var posithonIndex = this.GetCurrentIndex(e.GetPosition);

            if (posithonIndex != -1 && this.SelectionMode != System.Windows.Controls.SelectionMode.Single)
            {
                var posithonItem = this.Items[posithonIndex];

                if (this.SelectedItems.OfType<object>().Any(n => object.ReferenceEquals(n, posithonItem)))
                {
                    this.SelectedItems.Remove(posithonItem);
                    this.SelectedItems.Insert(0, posithonItem);
                }
            }
        }
    }
}

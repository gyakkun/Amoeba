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
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            {
                Point lposition = e.GetPosition(this);

                if ((this.ActualWidth - lposition.X) < 15
                    || (this.ActualHeight - lposition.Y) < 15)
                {
                    return;
                }
            }

            if (this.GetCurrentIndex(e.GetPosition) == -1)
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

            if (this.SelectionMode != System.Windows.Controls.SelectionMode.Single)
            {
                if (!System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                    && !System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    var selectedItems = this.SelectedItems.OfType<object>().ToList();

                    this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        var posithonIndex = this.GetCurrentIndex(e.GetPosition);
                        if (posithonIndex == -1) return;

                        var posithonItem = this.Items[posithonIndex];

                        if (selectedItems.Any(n => object.ReferenceEquals(n, posithonItem)))
                        {
                            this.SelectedItems.Clear();

                            foreach (var item in selectedItems)
                            {
                                this.SelectedItems.Add(item);
                            }
                        }
                        else
                        {
                            this.SelectedItems.Clear();
                            this.SelectedItems.Add(posithonItem);
                        }
                    }), null);
                }
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (this.SelectionMode != System.Windows.Controls.SelectionMode.Single)
            {
                if (!System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
                    && !System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    var posithonIndex = this.GetCurrentIndex(e.GetPosition);
                    if (posithonIndex == -1) return;

                    var selectedItems = this.SelectedItems.OfType<object>().ToList();
                    var posithonItem = this.Items[posithonIndex];

                    if (!selectedItems.Any(n => object.ReferenceEquals(n, posithonItem))) return;

                    this.SelectedItems.Clear();
                    this.SelectedItems.Add(this.Items[posithonIndex]);
                }
            }

            base.OnPreviewMouseLeftButtonUp(e);
        }
    }
}

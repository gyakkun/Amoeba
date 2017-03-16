using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Amoeba.Interface
{
    // http://b.starwing.net/?p=131
    public class DragStartBehavior : Behavior<FrameworkElement>
    {
        private Point _origin;
        private bool _isButtonDown;

        public DragDropEffects AllowedEffects
        {
            get { return (DragDropEffects)GetValue(AllowedEffectsProperty); }
            set { SetValue(AllowedEffectsProperty, value); }
        }

        public static readonly DependencyProperty AllowedEffectsProperty =
            DependencyProperty.Register("AllowedEffects", typeof(DragDropEffects),
                    typeof(DragStartBehavior), new UIPropertyMetadata(DragDropEffects.All));

        public object DragDropData
        {
            get { return GetValue(DragDropDataProperty); }
            set { SetValue(DragDropDataProperty, value); }
        }

        public static readonly DependencyProperty DragDropDataProperty =
            DependencyProperty.Register("DragDropData", typeof(object),
                    typeof(DragStartBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseDown += this.AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove += this.AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp += this.AssociatedObject_PreviewMouseUp;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseDown -= this.AssociatedObject_PreviewMouseDown;
            this.AssociatedObject.PreviewMouseMove -= this.AssociatedObject_PreviewMouseMove;
            this.AssociatedObject.PreviewMouseUp -= this.AssociatedObject_PreviewMouseUp;
        }

        void AssociatedObject_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _origin = e.GetPosition(this.AssociatedObject);
            _isButtonDown = true;
        }

        void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_isButtonDown)
            {
                return;
            }

            var point = e.GetPosition(this.AssociatedObject);

            if (CheckDistance(point, _origin))
            {
                if (this.DragDropData is IDragable dragObject)
                {
                    var data = new DataObject(dragObject.DragFormat, dragObject);
                    DragDrop.DoDragDrop(this.AssociatedObject, data, this.AllowedEffects);
                }

                _isButtonDown = false;
                e.Handled = true;
            }
        }

        void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isButtonDown = false;
        }

        private bool CheckDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }
    }
}

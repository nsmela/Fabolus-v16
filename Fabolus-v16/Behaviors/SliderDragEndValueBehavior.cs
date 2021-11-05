using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fabolus_v16.Behaviors {

    public class SliderDragEndValueBehavior : Behavior<Slider> {

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(float), typeof(SliderDragEndValueBehavior), new PropertyMetadata(default(float)));

        public float Value {
            get { return (float)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        protected override void OnAttached() {
            RoutedEventHandler handler = AssociatedObject_DragCompleted;
            AssociatedObject.AddHandler(Thumb.DragCompletedEvent, handler);
        }

        private void AssociatedObject_DragCompleted(object sender, RoutedEventArgs e) {
            Value = (float)AssociatedObject.Value;
        }

        protected override void OnDetaching() {
            RoutedEventHandler handler = AssociatedObject_DragCompleted;
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, handler);
        }
    }
}

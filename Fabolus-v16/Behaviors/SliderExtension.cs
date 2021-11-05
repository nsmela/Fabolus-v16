using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Fabolus_v16.Behaviors {
    public class SliderExtension {
        public static readonly DependencyProperty DragCompletedCommandProperty = DependencyProperty.RegisterAttached(
            "DragCompletedCommand",
            typeof(ICommand),
            typeof(SliderExtension),
            new PropertyMetadata(default(ICommand), OnDragCompletedCommandChanged));

        private static void OnDragCompletedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			if (d is not Slider slider) {
				return;
			}

			if (e.NewValue is ICommand) {
                slider.Loaded += SliderOnLoaded;
            }
        }

        private static void SliderOnLoaded(object sender, RoutedEventArgs e) {
			if (sender is not Slider slider) {
				return;
			}
			slider.Loaded -= SliderOnLoaded;

			if (slider.Template.FindName("PART_Track", slider) is not Track track) {
				return;
			}
			track.Thumb.DragCompleted += (dragCompletedSender, dragCompletedArgs) => {
                ICommand command = GetDragCompletedCommand(slider);
                command.Execute(null);
            };
        }

        public static void SetDragCompletedCommand(DependencyObject element, ICommand value) {
            element.SetValue(DragCompletedCommandProperty, value);
        }

        public static ICommand GetDragCompletedCommand(DependencyObject element) {
            return (ICommand)element.GetValue(DragCompletedCommandProperty);
        }

    }
}

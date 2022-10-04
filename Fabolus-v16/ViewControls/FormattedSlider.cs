using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fabolus_v16.ViewControls {
    public class FormattedSlider : Slider {

        //https://joshsmithonwpf.wordpress.com/2007/09/14/modifying-the-auto-tooltip-of-a-slider/

        private ToolTip _autoToolTip;
        public ToolTip AutoToolTip { 
            get { 
                if(_autoToolTip == null) {
                    FieldInfo field = typeof(Slider).GetField("_autoToolTip", BindingFlags.NonPublic | BindingFlags.Instance);
                    _autoToolTip = field.GetValue(this) as ToolTip;
                }
                return _autoToolTip;
            } 
        }

        private string _autoToolTipFormat;
        public string AutoToolTipFormat {get => _autoToolTipFormat; set => _autoToolTipFormat = value; }

        protected override void OnThumbDragStarted(DragStartedEventArgs e) {
            base.OnThumbDragStarted(e);
            this.FormatAutoToolTipContent();
        }
        protected override void OnThumbDragDelta(DragDeltaEventArgs e) {
            base.OnThumbDragDelta(e);
            this.FormatAutoToolTipContent();
        }

        private void FormatAutoToolTipContent() {
            if (!string.IsNullOrEmpty(_autoToolTipFormat)) {
                this.AutoToolTip.Content = string.Format(
                    this.AutoToolTipFormat,
                    this.AutoToolTip.Content);
            }
        }


    }
}

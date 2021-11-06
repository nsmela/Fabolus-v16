using Fabolus_v16.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Fabolus_v16.MVVM.ViewModels {

	#region Mold Type Enumerator
	public enum MoldTypes {
		box,
		flatbottom,
		flattop,
		contoured
	}

	public class MoldTypeEnumToIntValueConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			MoldTypes mold = (MoldTypes)(int)Math.Round((double)value);
			return mold.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	#endregion

	class MoldViewModel : ViewModelBase {
		private readonly BolusStore _bolusStore;

		private float _offsetDistance;
		public float OffsetDistance {
			get => _offsetDistance;
			set {
				_offsetDistance = value;
				OnPropertyChanged(nameof(OffsetDistance));
			}
		}

		private int _resolution;
		public int Resolution {
			get => _resolution;
			set {
				_resolution = value;
				OnPropertyChanged(nameof(Resolution));
			}
		}

		public MoldViewModel(MainViewModel mainViewModel) {
			_bolusStore = mainViewModel.MainBolusStore;
			_bolusStore.PreviewMoldVisibility = true;
			OffsetDistance = (float)_bolusStore.MoldOffset;
			Resolution = _bolusStore.MoldResolution;
		}

		public override void OnExit() {
			_bolusStore.PreviewMoldVisibility = false;
			base.OnExit();
		}
	}
}

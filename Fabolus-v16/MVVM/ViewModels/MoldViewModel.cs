using Fabolus_v16.Commands;
using Fabolus_v16.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

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

		public ICommand OffsetDistanceSliderDragCompleteCommand { get; }
		private void OffsetDistanceSliderDragComplete() {
			if (_bolusStore.MoldOffset == _offsetDistance)
				return; //don't update and waste time

			_bolusStore.MoldOffset = _offsetDistance; //will update mold
		}

		private int _resolution;
		public int Resolution {
			get => _resolution;
			set {
				_resolution = value;
				OnPropertyChanged(nameof(Resolution));
			}
		}

		public ICommand ResolutionSliderDragCompleteCommand { get; }
		private void ResolutionSliderDragComplete() {
			if (_bolusStore.MoldResolution == _resolution)
				return;//don't update and waste time

			_bolusStore.MoldResolution = _resolution; //will update mold
		}

		public MoldViewModel(MainViewModel mainViewModel) {
			_bolusStore = mainViewModel.MainBolusStore;
			_bolusStore.PreviewMoldVisibility = true;
			OffsetDistance = (float)_bolusStore.MoldOffset;
			Resolution = _bolusStore.MoldResolution;

			OffsetDistanceSliderDragCompleteCommand = new RelayCommand(param => this.OffsetDistanceSliderDragComplete(), param => true);
			ResolutionSliderDragCompleteCommand = new RelayCommand(param => this.ResolutionSliderDragComplete(), param => true);
		}

		public override void OnExit() {
			_bolusStore.PreviewMoldVisibility = false;
			base.OnExit();
		}
	}
}

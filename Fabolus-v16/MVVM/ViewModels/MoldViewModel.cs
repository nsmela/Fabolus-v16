using Fabolus_v16.Commands;
using Fabolus_v16.Stores;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Fabolus_v16.MVVM.ViewModels {

	#region Mold Type Enumerator
	public enum MoldTypes {
		[Description("")] NOT_SET,
		[Description("Uniform Box")] BOX,
		[Description("Flatten Bottom")] FLATBOTTOM,
		[Description("Flatten Top")] FLATTOP,
		[Description("Contour")] CONTOURED
	}

	public class MoldTypeEnumToIntValueConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			MoldTypes mold = (MoldTypes)(int)Math.Round((double)value);
			//MoldTypes mold = (MoldTypes)(value);
			//var mold = value;
			var attributes = mold.GetType().GetField(mold.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (attributes.Any())
				return (attributes.First() as DescriptionAttribute).Description;

			return mold.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}
	}

	#endregion

	class MoldViewModel : ViewModelBase {
		private readonly BolusStore _bolusStore;
		private readonly AirChannelStore _airChannelStore;

		private float _offsetDistance;
		public float OffsetDistance {
			get => _offsetDistance;
			set {
				_offsetDistance = value;
				OnPropertyChanged(nameof(OffsetDistance));
			}
		}

		#region Button Commands and Values
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

		private int _moldShape;
		public int MoldShape {
			get => _moldShape;
			set {
				_moldShape = value;
				OnPropertyChanged(nameof(MoldShape));
			}
		}
		public ICommand MoldTypeSliderDragCompleteCommand { get; }
		private void MoldTypeSliderDragComplete() {
			if ((int)_bolusStore.MoldType == _moldShape)
				return;//don't update and waste time

			_bolusStore.MoldType = (MoldTypes)_moldShape; //will update mold
		}

		public List<MoldTypes> MoldShapes {
			get;
			set;
		}

		#region Scale
		private double _scaleX;
		public double ScaleX { get => _scaleX; set { _scaleX = value; OnPropertyChanged(nameof(ScaleX)); } }
		public ICommand ScaleXSliderDragCompleteCommand { get; }
		private void ScaleXSliderDragComplete() {
			if (_bolusStore.MoldScaleX == _scaleX)
				return;//don't update and waste time

			_bolusStore.MoldScaleX = _scaleX; //will update mold
		}

		private double _scaleY;
		public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(nameof(ScaleY)); } }
		public ICommand ScaleYSliderDragCompleteCommand { get; }
		private void ScaleYSliderDragComplete() {
			if (_bolusStore.MoldScaleY == _scaleY)
				return;//don't update and waste time

			_bolusStore.MoldScaleY = _scaleY; //will update mold
		}

		private double _scaleZ;
		public double ScaleZ { get => _scaleZ; set { _scaleZ = value; OnPropertyChanged(nameof(ScaleZ)); } }
		public ICommand ScaleZSliderDragCompleteCommand { get; }
		private void ScaleZSliderDragComplete() {
			if (_bolusStore.MoldScaleZ == _scaleZ)
				return;//don't update and waste time

			_bolusStore.MoldScaleZ = _scaleZ; //will update mold
		}
		#endregion

		#region Translate
		private double _translateX;
		public double TranslateX { get => _translateX; set { _translateX = value; OnPropertyChanged(nameof(TranslateX)); } }
		public ICommand TranslateXSliderDragCompleteCommand { get; }
		private void TranslateXSliderDragComplete() {
			if (_bolusStore.MoldTranslateX == _translateX)
				return;//don't update and waste time

			_bolusStore.MoldTranslateX = _translateX; //will update mold
		}

		private double _translateY;
		public double TranslateY { get => _translateY; set { _translateY = value; OnPropertyChanged(nameof(TranslateY)); } }
		public ICommand TranslateYSliderDragCompleteCommand { get; }
		private void TranslateYSliderDragComplete() {
			if (_bolusStore.MoldTranslateY == _translateY)
				return;//don't update and waste time

			_bolusStore.MoldTranslateY = _translateY; //will update mold
		}

		private double _translateZ;
		public double TranslateZ { get => _translateZ; set { _translateZ = value; OnPropertyChanged(nameof(TranslateZ)); } }
		public ICommand TranslateZSliderDragCompleteCommand { get; }
		private void TranslateZSliderDragComplete() {
			if (_bolusStore.MoldTranslateZ == _translateZ)
				return;//don't update and waste time

			_bolusStore.MoldTranslateZ = _translateZ; //will update mold
		}
		#endregion

		public ICommand GenerateMoldCommand { get; }
		private void GenerateMold() {
			_airChannelStore.Visibility = false;
			_bolusStore.GenerateMold(_airChannelStore.AirChannels);
			
		}

		#endregion

		public MoldViewModel(MainViewModel mainViewModel) {
			mainViewModel.CurrentViewTitle = "mold tool";
			_bolusStore = mainViewModel.MainBolusStore;
			_bolusStore.PreviewMoldVisibility = true;
			_airChannelStore = mainViewModel.MainAirChannelStore;
			OffsetDistance = (float)_bolusStore.MoldOffset;
			Resolution = _bolusStore.MoldResolution;
			MoldShape = (int)_bolusStore.MoldType;

			MoldShapes = new List<MoldTypes>();
			MoldShapes.Add(MoldTypes.BOX);
			MoldShapes.Add(MoldTypes.FLATTOP);
			MoldShapes.Add(MoldTypes.FLATBOTTOM);
			MoldShapes.Add(MoldTypes.CONTOURED);


			ScaleX = _bolusStore.MoldScaleX;
			ScaleY = _bolusStore.MoldScaleY;
			ScaleZ = _bolusStore.MoldScaleZ;

			TranslateX = _bolusStore.MoldTranslateX;
			TranslateY = _bolusStore.MoldTranslateY;
			TranslateZ = _bolusStore.MoldTranslateZ;

			OffsetDistanceSliderDragCompleteCommand = new RelayCommand(param => this.OffsetDistanceSliderDragComplete(), param => true);
			ResolutionSliderDragCompleteCommand = new RelayCommand(param => this.ResolutionSliderDragComplete(), param => true);
			MoldTypeSliderDragCompleteCommand = new RelayCommand(param => this.MoldTypeSliderDragComplete(), param => true);

			ScaleXSliderDragCompleteCommand = new RelayCommand(param => this.ScaleXSliderDragComplete(), param => true);
			ScaleYSliderDragCompleteCommand = new RelayCommand(param => this.ScaleYSliderDragComplete(), param => true);
			ScaleZSliderDragCompleteCommand = new RelayCommand(param => this.ScaleZSliderDragComplete(), param => true);

			TranslateXSliderDragCompleteCommand = new RelayCommand(param => this.TranslateXSliderDragComplete(), param => true);
			TranslateYSliderDragCompleteCommand = new RelayCommand(param => this.TranslateYSliderDragComplete(), param => true);
			TranslateZSliderDragCompleteCommand = new RelayCommand(param => this.TranslateZSliderDragComplete(), param => true);

			GenerateMoldCommand = new RelayCommand(param => this.GenerateMold(), param => true);
		}

		public override void OnExit() {
			_bolusStore.PreviewMoldVisibility = false;
			_bolusStore.BolusMold = null;
			base.OnExit();
		}
	}
}

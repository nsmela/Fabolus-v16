using Fabolus_v16.Commands;
using Fabolus_v16.MVVM.Models;
using Fabolus_v16.Stores;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace Fabolus_v16.MVVM.ViewModels {
	public enum SmoothingValue {
		[Description("Rough")] rough = 0,
		[Description("Standard")] standard = 1,
		[Description("Smoothest")] smoothest = 2,
	}

	public static class SmoothEnumExtension {
		public static string ToDescriptionString(this SmoothingValue value) {
			DescriptionAttribute[] attributes = (DescriptionAttribute[])value
				.GetType()
				.GetField(value.ToString())
				.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return attributes.Length > 0 ? attributes[0].Description : string.Empty;
		}
	}

	public class SmoothViewModel :ViewModelBase {
		private BolusStore _bolusStore;

		#region Commands
		public ICommand SmoothCommand { get; }
		private static bool CanApplySmooth() { return true; }
		private void SmoothBolus() {
			if (_bolusStore.BolusRaw == null)
				return;

			if (_marchingCubesMode) {
				_bolusStore.BolusSmoothed = new Bolus(BolusTools.Smooth(_edgeSize, _smoothSpeed, (double)_iterations, (double)_marchingCubes, _bolusStore.BolusRaw));

			} else {
				_bolusStore.BolusSmoothed = new Bolus(BolusTools.PoissonSmoothing(_bolusStore.BolusRaw.DMesh, _depth, _scale, _samplesPerNode));
			}

			_bolusStore.PreviewMoldVisibility = false;
		}

		public ICommand ClearSmoothedBolusCommand { get; }
		private bool CanClearSmoothedBolus() { return true; }
		private void ClearSmoothedBolus() { _bolusStore.BolusSmoothed = null; }
		#endregion

		#region Simple Smooth Setting
		private SmoothingValue _smoothSetting;
		private bool _customSetting;

		public string SmoothSettingLabel {
			get {
				if (_customSetting) return "Custom";
				else return _smoothSetting.ToDescriptionString();
			}
		}

		public int SmoothingSettingInt { 
			get => (int)_smoothSetting; 
			set { 
				_smoothSetting = (SmoothingValue)value; 
				DefaultSmoothSetting((SmoothingValue)value);
				_customSetting = false;
				OnPropertyChanged(nameof(SmoothSettingLabel)); 
				OnPropertyChanged(nameof(SmoothingSettingInt)); 
			} 
		}

		public bool CustomSmoothSettings {
			get => _customSetting;
			set {
				_customSetting = value;
				OnPropertyChanged(nameof(SmoothSettingLabel));
			}
		}
		#endregion

		#region Smoothing Settings
		private float _edgeSize, _smoothSpeed;
		private int _iterations, _marchingCubes;

		private int _degrees, _depth, _samplesPerNode;
		private float _scale;

		private bool _poissonMode = true, _marchingCubesMode = false;

		public float EdgeSize { get => _edgeSize; set { _edgeSize = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(EdgeSize)); } }
		public float SmoothSpeed { get => _smoothSpeed; set { _smoothSpeed = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(SmoothSpeed)); } }
		public int Iterations { get => _iterations; set { _iterations = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(Iterations)); } }
		public int MarchingCubes { get => _marchingCubes; set { _marchingCubes = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(MarchingCubes)); } }

		public int Degrees { get => _degrees; set { _degrees = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(Degrees)); } }
		public int Depth { get => _depth; set { _depth = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(Depth)); } }
		public int SamplesPerNode { get => _samplesPerNode; set { _samplesPerNode = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(SamplesPerNode)); } }
		public float Scale { get => _scale; set { _scale = value; CustomSmoothSettings = true; OnPropertyChanged(nameof(Scale)); } }

		public bool PoissonMode { get => _poissonMode; set => _poissonMode = value; }
		public bool MarchingCubesMode { get => _marchingCubesMode; set => _marchingCubesMode = value; }

		//default values for the smoothing settings enum
		private void DefaultSmoothSetting(SmoothingValue value) {
			switch (value) {
				case SmoothingValue.rough:
					EdgeSize = 0.2f;
					SmoothSpeed = 0.2f;
					Iterations = 1;
					MarchingCubes = 32;

					Degrees = 1;
					Depth = 6;
					Scale = 1.2f;
					SamplesPerNode = 1;
					break;

				case SmoothingValue.standard:
					EdgeSize = 0.4f;
					SmoothSpeed = 0.2f;
					Iterations = 1;
					MarchingCubes = 64;

					Degrees = 1;
					Depth = 6;
					Scale = 1.2f;
					SamplesPerNode = 1;
					break;

				case SmoothingValue.smoothest:
					EdgeSize = 0.6f;
					SmoothSpeed = 0.4f;
					Iterations = 2;
					MarchingCubes = 128;

					Degrees = 1;
					Depth = 6;
					Scale = 1.2f;
					SamplesPerNode = 1;
					break;
			}
}


		#endregion

		public SmoothViewModel(MainViewModel mainViewmodel, BolusStore bolusStore) {
			mainViewmodel.CurrentViewTitle = "smooth bolus";

			_bolusStore = bolusStore;
			SmoothingSettingInt = (int)SmoothingValue.standard;

			//commands
			SmoothCommand = new RelayCommand(param => this.SmoothBolus(), param => CanApplySmooth());
			ClearSmoothedBolusCommand = new RelayCommand(param => this.ClearSmoothedBolus(), param => CanClearSmoothedBolus());


		}
	}
}

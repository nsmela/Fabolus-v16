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
	public class RotateViewModel : ViewModelBase {
		private BolusStore _bolusStore;
		private MeshViewModel _meshViewModel;

		#region Commands
		public ICommand SaveRotationCommand { get; }
		public ICommand ClearRotationCommand { get; }
		#endregion


		public RotateViewModel(MainViewModel mainViewModel, BolusStore bolusStore) {
			mainViewModel.CurrentViewTitle = "rotation";
			_bolusStore = bolusStore;
			_meshViewModel = mainViewModel.MeshVM;

			SaveRotationCommand = new RelayCommand(param => this.SaveRotation(), param => true);
			ClearRotationCommand = new RelayCommand(param => this.ResetRotation(), param => true) ;

		}

		#region Rotation
		private float _xAxisAngle, _yAxisAngle, _zAxisAngle;
		private Vector3D _axisVector = new(0, 0, 0);
		public float XAxisRotation {
			get => _xAxisAngle;
			set {
				_xAxisAngle = value; //to make sure the slider doesn't change it's value during an update
				if (_axisVector.X == 0) //reducing workload by checking if new vector needed
					_axisVector = new Vector3D(1, 0, 0);

				_bolusStore.BolusRotation = new AxisAngleRotation3D(_axisVector, _xAxisAngle);
				OnBolusRotationChanged();
			}
		}
		public float YAxisRotation {
			get => _yAxisAngle;
			set {
				_yAxisAngle = value; //to make sure the slider doesn't change it's value during an update
				if (_axisVector.Y == 0) //reducing workload by checking if new vector needed
					_axisVector = new Vector3D(0, 1, 0);

				_bolusStore.BolusRotation = new AxisAngleRotation3D(_axisVector, _yAxisAngle);
				OnBolusRotationChanged();
			}
		}
		public float ZAxisRotation {
			get => _zAxisAngle;
			set {
				_zAxisAngle = value; //to make sure the slider doesn't change it's value during an update
				if (_axisVector.Z == 0) //reducing workload by checking if new vector needed
					_axisVector = new Vector3D(0, 0, 1);

				_bolusStore.BolusRotation = new AxisAngleRotation3D(_axisVector, _zAxisAngle);
				OnBolusRotationChanged();
			}
		}

		public void ResetRotation() {
			_bolusStore.ClearBolusTransforms();
			OnPropertyChanged(nameof(_meshViewModel.BolusMesh));
		}

		private void OnBolusRotationChanged() {
			OnPropertyChanged(nameof(XAxisRotation));
			OnPropertyChanged(nameof(YAxisRotation));
			OnPropertyChanged(nameof(ZAxisRotation));

			//clear the airholes
			_meshViewModel.ClearAirChannels();

			/*update mold 
			if (ShowMold) {
				_bolusStore.PreviewMold();
				OnPropertyChanged(nameof(MoldMesh));
			}*/
			
			OnPropertyChanged(nameof(_meshViewModel.BolusMesh));
			
		}

		public void SaveRotation() {
			//find which axis angle was active
			float angle = 0f;
			if (_xAxisAngle != 0) angle = _xAxisAngle;
			if (_yAxisAngle != 0) angle = _yAxisAngle;
			if (_zAxisAngle != 0) angle = _zAxisAngle;

			//save the slider's adjustment to the bolus
			var rotate = new AxisAngleRotation3D(_axisVector, angle);
			var rotationTransform = new RotateTransform3D(rotate);
			_bolusStore.AddTransform(rotationTransform);

			//reset slider object
			_xAxisAngle = 0f;
			_yAxisAngle = 0f;
			_zAxisAngle = 0f;
			_axisVector = new Vector3D(0, 0, 0);
			OnBolusRotationChanged();

			//reset BolusStore rotation, will also update bolus properties
			_bolusStore.BolusRotation = new AxisAngleRotation3D(_axisVector, 0f);

		}
		#endregion

	}
}

using Fabolus_v16.Commands;
using Fabolus_v16.MVVM.Models;
using Fabolus_v16.Stores;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus_v16.MVVM.ViewModels {
	public class AirChannelsViewModel : ViewModelBase {
		private AirChannelStore _airChannelStore;
		private MeshViewModel _meshVM;

		public ICommand ClearAirChannelsCommand { get; }

		public AirChannelsViewModel(MainViewModel mainViewModel) {
			mainViewModel.CurrentViewTitle = "air channel tool";
			_airChannelStore = mainViewModel.MainAirChannelStore;

			_airChannelStore.Visibility = true;
			_channelDiameter = _airChannelStore.PreviewDiameter;
			_meshVM = mainViewModel.MeshVM;

			_meshVM.MeshMouseDown += OnMeshHit;
			_meshVM.MeshMouseMove += OnMeshMove;
			
			ClearAirChannelsCommand = new RelayCommand(param => this.ClearAirChannels(), param => true);

			_airChannelStore.PreviewDiameter = _channelDiameter;
			_airChannelStore.PreviewHeight = _meshVM.BolusHeight;
		}
		public override void OnExit() {
			_airChannelStore.Visibility = false;
			_meshVM.MeshMouseDown -= OnMeshHit;
			_meshVM.MeshMouseMove -= OnMeshMove;
		}

		private float _channelDiameter;
		public float ChannelDiameter {
			get => _channelDiameter;
			set {
				_channelDiameter = value;
				_airChannelStore.PreviewDiameter = _channelDiameter;
				OnPropertyChanged(nameof(ChannelDiameter));
			}
		}

		#region Air Channel Mesh
		private void AddAirChannel(Point3D point) {
			AirChannel a = new AirChannel(point, _channelDiameter, _meshVM.BolusHeight - (float)point.Z + 20);
			_airChannelStore.AddChannel(a);
			_meshVM.SetLowestAirChannelPoint(_airChannelStore.LowestAirChannel);
		}

		public void ClearAirChannels() {
			_airChannelStore.ClearAirChannels();
			_meshVM.SetLowestAirChannelPoint(_airChannelStore.LowestAirChannel);
		}
		#endregion

		#region Mouse Methods
		private void OnMeshHit() {
			var meshHit = _meshVM.MeshHit;
			if (meshHit == null)
				return;

			AddAirChannel(meshHit.PointHit);
		}

		private void OnMeshMove() {
			var meshHit = _meshVM.MeshHit;
			if (meshHit == null)
				return;

			_airChannelStore.PreviewPoint = meshHit.PointHit;
		}
		#endregion
	}
}

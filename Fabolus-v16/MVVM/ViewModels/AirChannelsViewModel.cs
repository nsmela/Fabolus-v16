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
			_channelRadius = 2.5f;
			_meshVM = mainViewModel.MeshVM;

			_meshVM.MeshMouseDown += OnMeshHit;
			_meshVM.MeshMouseMove += OnMeshMove;
			
			ClearAirChannelsCommand = new RelayCommand(param => this.ClearAirChannels(), param => true);

			_airChannelStore.PreviewRadius = _channelRadius;
			_airChannelStore.PreviewHeight = 50f;
		}
		public override void OnExit() {
			_airChannelStore.Visibility = false;
			_meshVM.MeshMouseDown -= OnMeshHit;
			_meshVM.MeshMouseMove -= OnMeshMove;
		}

		private float _channelRadius;
		public float ChannelRadius {
			get => _channelRadius;
			set {
				_channelRadius = value;
				_airChannelStore.PreviewRadius = _channelRadius;
				OnPropertyChanged(nameof(ChannelRadius));
			}
		}

		#region Air Channel Mesh
		private void AddAirChannel(Point3D point) {
			AirChannel a = new AirChannel(point, _channelRadius, 50f);
			_airChannelStore.AddChannel(a);
		}

		public void ClearAirChannels() {
			_airChannelStore.ClearAirChannels();
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

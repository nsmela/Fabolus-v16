using Fabolus_v16.MVVM.Models;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus_v16.Stores {
	public class AirChannelStore {
		private List<AirChannel> _airChannels;
		private Color _color;
		private float _opacity;
		private bool _visibility;

		#region events
		public event Action AirChannelsChanged;
		private void OnAirChannelsChanged() {
			AirChannelsChanged?.Invoke();
		}
		public event Action PreviewChannelChanged;
		private void OnPreviewChannelChanged() {
			PreviewChannelChanged?.Invoke();
		}

		#endregion

		public Color Color { get => _color; set => _color = value; }
		public float Opacity { get => _opacity; set => _opacity = value; }
		public int Count { get => _airChannels.Count; }
		public bool Visibility { get => _visibility; set => _visibility = value; }
		public List<AirChannel> AirChannels { get => _airChannels; }
		public GeometryModel3D AirChannelsMesh {
			get {
				MeshBuilder mesh = new MeshBuilder(true);

				foreach (AirChannel a in _airChannels) {
					mesh.Append(a.Mesh);
				}

				//material
				var skin = new DiffuseMaterial(new SolidColorBrush(Color));
				skin.Brush.Opacity = Opacity;
				var result = new GeometryModel3D(mesh.ToMesh(), skin);
				result.BackMaterial = skin;

				return result;
			}

		}

		public void AddChannel(AirChannel airChannel) {
			_airChannels.Add(airChannel);
			OnAirChannelsChanged();
		}

		public void ClearAirChannels() {
			_airChannels.Clear();
			OnAirChannelsChanged();
		}

		public double LowestAirChannel { get => _airChannels.Any() ? _airChannels.Min(airchannel => airchannel.Anchor.Z) : -1000; }

		#region Preview Airhole
		private Point3D _previewPoint;
		public Point3D PreviewPoint {
			set {
				_previewPoint = value;
				_previewVisible = true;
				OnPreviewChannelChanged();
			}
		}

		private float _previewDiameter;
		public float PreviewDiameter {
			get => _previewDiameter;
			set {
				_previewDiameter = value;
				OnPreviewChannelChanged();
			}
		}

		private float _previewHeight;
		public float PreviewHeight {
			set {
				_previewHeight = value;
				OnPreviewChannelChanged();
			}
		}

		private bool _previewVisible;
		public bool PreviewVisible {
			set {
				_previewVisible = value;
				OnPreviewChannelChanged();
			}
		}

		public GeometryModel3D PreviewAirChannel {
			get {
				if (!Visibility || 
					!_previewVisible) 
					return null;

				//material
				var skin = new DiffuseMaterial(new SolidColorBrush(Color));
				skin.Brush.Opacity = Opacity;
				var result = new GeometryModel3D(
					new AirChannel(_previewPoint, _previewDiameter, _previewHeight).Mesh, 
					skin);
				result.BackMaterial = skin;

				return result;
			}
		}
		#endregion
		
		public AirChannelStore() {
			_airChannels = new List<AirChannel>();
			Color = Colors.Blue;
			Opacity = 1.0f;
			_previewDiameter = 5.0f;
		}



	}
}

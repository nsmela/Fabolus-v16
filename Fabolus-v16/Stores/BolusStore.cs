using Fabolus_v16.MVVM.Models;
using Fabolus_v16.MVVM.ViewModels;
using g3;
using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
namespace Fabolus_v16.Stores {

	public class BolusStore {
		private Bolus _bolus;
		private Bolus _smoothedBolus;
		private Bolus _moldBolus;
		private AxisAngleRotation3D _tempRotation = new();
		private Color _meshColor = Colors.Gray;
		private float _opacity = 1.0f;

		public Bolus CurrentBolus {
			get {
				if (_smoothedBolus != null)
					return _smoothedBolus;

				return _bolus;
			}
			set {
				_bolus = value;
				_smoothedBolus = null;
				_moldBolus = null;
				OnCurrentBolusChanged();
			}
		}

		public Bolus BolusRaw { get => _bolus; set => CurrentBolus = value; }
		public Bolus BolusSmoothed { get => _smoothedBolus; set { _smoothedBolus = value; _moldBolus = null; OnCurrentBolusChanged(); } }
		public Bolus BolusMold { get => _moldBolus; set { _moldBolus = value; OnCurrentBolusChanged(); } }

		public event Action CurrentBolusChanged;
		public event Action MoldChanged;
		public event Action BolusRotate;

		private void OnCurrentBolusChanged() {
			CurrentBolusChanged?.Invoke();
		}
		private void OnMoldChanged() {
			MoldChanged?.Invoke();
		}

		private void OnBolusRotate() {
			BolusRotate?.Invoke();
		}

		public String BolusVolume {
			get {
				if (CurrentBolus == null)
					return "No model loaded";

				string text = "";

				string rawVolume = BolusTools.CalculateVolumeText(BolusRaw.DMesh);
				text += "Load Bolus Volume: " + rawVolume + " mL";

				if (BolusSmoothed != null) {
					string smoothVolume = BolusTools.CalculateVolumeText(BolusSmoothed.DMesh);
					text += "      Smoothed Bolus Volume: " + smoothVolume + " mL";
				}

				return text;
			}
		}

		#region Bolus to viewport Object
		public Model3DGroup BolusModel3D {
			get {
				Model3DGroup meshes = new Model3DGroup();
				meshes.Children.Add(BolusModelGeometry3D);
				meshes.Children.Add(Overhangs);

				return meshes;
			}
		}

		private GeometryModel3D BolusModelGeometry3D { get => MeshToGeometry(CurrentBolus.MeshGeometry, _meshColor, _opacity); }
		//private GeometryModel3D MoldModelGeometry3D { get => MeshToGeometry(_moldBolus.MeshGeometry, _moldColor, _opacity / 2, false); }

		private GeometryModel3D MeshToGeometry(MeshGeometry3D mesh, Color color, float opacity, bool transforms = true) {
			//material
			var skin = new DiffuseMaterial(new SolidColorBrush(color));
			skin.Brush.Opacity = opacity;

			//transforms
			Transform3DGroup transformGroup = _bolusTransform.Clone();
			transformGroup.Children.Add(new RotateTransform3D(_tempRotation)); //from bolusStore
			var result = new GeometryModel3D(mesh, skin);
			result.BackMaterial = skin;

			if (transforms) result.Transform = transformGroup;

			return result;
		}

		private bool _showOverhangs = false;
		public bool ShowOverhangs {
			get => _showOverhangs;
			set {
				_showOverhangs = value;
				OnCurrentBolusChanged();
			}
		}

		public GeometryModel3D Overhangs {
			get {
				Transform3DGroup transformGroup = _bolusTransform.Clone();
				transformGroup.Children.Add(new RotateTransform3D(_tempRotation)); //from bolusStore
				Vector3D reference = transformGroup.Transform(new Vector3D(0, 0, -1));
				reference.Negate();
				var mesh = CurrentBolus.OverhangMesh(transformGroup, 60f);
				return MeshToGeometry(mesh, Colors.Yellow, 0.9f);
			}
		}

		public void SetBolusSkin(Color color, float opacity) {
			_meshColor = color;
			_opacity = opacity;
			OnCurrentBolusChanged();
		}
		#endregion

		#region Rotation and Transforms
		public AxisAngleRotation3D BolusRotation {
			set {
				_tempRotation = value;
				OnBolusRotate();
				OnCurrentBolusChanged();
			}
		}

		private Transform3DGroup _bolusTransform = new();

		public Transform3DGroup TransformGroup { get => _bolusTransform.Clone(); }
		public void AddTransform(RotateTransform3D rotation) {
			_bolusTransform.Children.Add(rotation);
			OnCurrentBolusChanged();
		}
		public void ClearBolusTransforms() {
			_bolusTransform.Children.Clear();

			if (ShowMold)
				PreviewMold();
			else
				ClearMoldMesh();

			OnBolusRotate();
			OnCurrentBolusChanged();
		}

		#endregion

		#region Mold
		private bool _showMold = false;
		private GeometryModel3D _moldMesh = new();
		private Color _moldColor = Colors.DarkViolet;
		private double _moldSize = 2;

		public bool ShowMold {
			get => _showMold;
			set {
				_showMold = value;

				if (_showMold)
					PreviewMold();
				else
					ClearMoldMesh();
			}
		}
		public GeometryModel3D MoldMesh {
			get => _moldMesh;
			set {
				_moldMesh = value;
				OnMoldChanged();
			}
		}

		public double MoldSize {
			get => _moldSize;
			set {
				_moldSize = value;
				if (ShowMold) {
					PreviewMold();
					OnMoldChanged();
				}
			}
		}

		public void PreviewMold() {
			if (CurrentBolus == null) return;

			var mesh = BolusTools.GenerateBolusMold(CurrentBolus.MeshGeometry, MoldSize, TransformGroup.Value);
			DiffuseMaterial material = new(new SolidColorBrush(_moldColor));
			material.Brush.Opacity = 0.3f;
			MoldMesh = new GeometryModel3D(mesh, material) { BackMaterial = material };
		}

		/*
		public void GenerateMold() {
			if (MoldViewModel.Instance.AirChannelMesh == null)
				return;

			var airChannelMesh = BolusTools.MeshGeometryToDMesh(MoldViewModel.Instance.AirChannelMesh);
			DMesh3 cavityMesh = BolusTools.MeshGeometryToDMesh(CurrentBolus.MeshGeometry, TransformGroup.Value);

			if (airChannelMesh.TriangleCount > 0) //boolean union will fail if either mesh is null
				cavityMesh = BolusTools.BooleanUnion(cavityMesh, airChannelMesh);

			var contourGeometry = BolusTools.GenerateBolusMold(CurrentBolus.MeshGeometry, MoldSize, TransformGroup.Value);
			var contourMesh = BolusTools.MeshGeometryToDMesh(contourGeometry);
			var mold = BolusTools.BooleanSubtraction(contourMesh, cavityMesh);

			if (mold.TriangleCount < 1)
				return;

			MoldViewModel.Instance.ShowMold = false;

			BolusMold = new Bolus(mold);
			OnCurrentBolusChanged();
		}*/
		public void ClearMoldMesh() {
			_moldMesh = new();
			OnMoldChanged();
		}

		public void UpdateMold() {
			OnMoldChanged();
		}
		#endregion
	}
}
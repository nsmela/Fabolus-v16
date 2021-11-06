using Fabolus_v16.MVVM.Models;
using Fabolus_v16.MVVM.ViewModels;
using g3;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
namespace Fabolus_v16.Stores {

	public class BolusStore {
		private Bolus _bolus;
		private Bolus _smoothedBolus;
		private Bolus _moldBolus;
		private AxisAngleRotation3D _tempRotation = new();
		private Color _meshColor = Colors.Gray;
		private float _bolusOpacity = 1.0f;

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

		private GeometryModel3D BolusModelGeometry3D { get => MeshToGeometry(CurrentBolus.MeshGeometry, _meshColor, _bolusOpacity); }

		private GeometryModel3D MeshToGeometry(MeshGeometry3D mesh, Color color, float opacity, bool transforms = true) {
			//material
			var skin = new DiffuseMaterial(new SolidColorBrush(color));
			skin.Brush.Opacity = opacity;

			//transforms
			Transform3DGroup transformGroup = TransformGroup;
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
			_bolusOpacity = opacity;
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

		private DMesh3 ApplyBolusRotation(DMesh3 mesh) {
			DMesh3 result = new DMesh3(mesh); //copy the mesh to prevent transforming the old one
			foreach(var q in _bolusRotation)
				MeshTransforms.Rotate(result, Vector3d.Zero, q);

			return result;
		}

		private Transform3DGroup _bolusTransform = new();
		private List<Quaterniond> _bolusRotation;

		public Transform3DGroup TransformGroup { get => _bolusTransform.Clone(); }
		
		public void AddTransform(Vector3D axis, double angle) {
			//rotation for DMesh3
			if (_bolusRotation == null)
				_bolusRotation = new List<Quaterniond>();

			_bolusRotation.Add(new Quaterniond(new Vector3d(axis.X, axis.Y, axis.Z), angle));

			var rotate = new AxisAngleRotation3D(axis, angle);
			_bolusTransform.Children.Add(new RotateTransform3D(rotate));
			OnCurrentBolusChanged();
		}
		public void ClearBolusTransforms() {
			_bolusTransform.Children.Clear();
			_bolusRotation = new List<Quaterniond>();

			OnBolusRotate();
			OnCurrentBolusChanged();
			OnMoldChanged();
		}

		#endregion

		#region Mold
		/* mold isn't generated until user enters MoldView
		 * toggle button to enable preview
		 * mold type is set by enum
		 * preview does not do any boolean operators
		 * generation uses boolean subtraction and hides the airholes, airhole preview, and current mesh
		 * any change clears generated mesh and creates preview mesh
		 */
		private bool _previewMold = false;
		private double _moldOffset = 2.5f;
		private int _moldResolution = 64;
		private float _moldOpacity = 0.3f;
		private Color _moldColor = Colors.Violet;
		private MoldTypes _moldType = 0;

		public bool PreviewMoldVisibility { set { _previewMold = value;	OnMoldChanged(); } }
		public double MoldOffset { get => _moldOffset;  set { _moldOffset = value; OnMoldChanged(); } }
		public int MoldResolution { get => _moldResolution; set { _moldResolution = value; OnMoldChanged(); } }
		public float MoldOpacity { get => _moldOpacity; set { _moldOpacity = value; OnMoldChanged(); } }
		public Color MoldColor { get => _moldColor; set { _moldColor = value; OnMoldChanged(); } }
		public MoldTypes MoldType { get => _moldType; set { _moldType = value; OnMoldChanged(); } }

		public GeometryModel3D MoldModel3D {
			get {
				if (CurrentBolus == null ||
					!_previewMold)
					return null;

				//apply transform
				DMesh3 rotated_mesh = ApplyBolusRotation(CurrentBolus.DMesh);

				var mesh = BolusTools.GenerateMold(rotated_mesh, _moldOffset);
				DiffuseMaterial material = new(new SolidColorBrush(_moldColor));
				material.Brush.Opacity = _moldOpacity;
				return new GeometryModel3D(BolusTools.DMeshToMeshGeometry(mesh), material) { BackMaterial = material };
			}
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

		#endregion
	}
}
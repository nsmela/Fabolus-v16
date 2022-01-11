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
				if (_moldBolus != null)
					return _moldBolus;

				if (_smoothedBolus != null)
					return _smoothedBolus;

				return _bolus;
			}
			set {
				_bolus = value;
				_smoothedBolus = null;
				_moldBolus = null;
				PreviewMoldVisibility = false;
				_airChannelStore.ClearAirChannels();
				ClearBolusTransforms();
			}
		}

		public DMesh3 CurrentBolusTransformed {
			get {
				if (_moldBolus != null)
					return _moldBolus.DMesh;

				return ApplyBolusRotation(CurrentBolus.DMesh);
			}
		}

		public Bolus BolusRaw { get => _bolus; set => CurrentBolus = value; }
		public Bolus BolusSmoothed { get => _smoothedBolus; set { _smoothedBolus = value; _moldBolus = null; OnCurrentBolusChanged(); } }
		public Bolus BolusMold { get => _moldBolus; set { _moldBolus = value; OnMoldFinalChanged(); } }

		#region events
		public event Action CurrentBolusChanged;
		public event Action MoldPreviewChanged;
		public event Action MoldFinalChanged;
		public event Action BolusRotate;
		public event Action MoldScaleChanged;

		private void OnCurrentBolusChanged() {
			CurrentBolusChanged?.Invoke();
		}
		private void OnMoldPreviewChanged() {
			if (CurrentBolus != null && _previewMold)
				GeneratePreviewMold();

			MoldPreviewChanged?.Invoke();
		}

		private void OnMoldFinalChanged() {
			if (_moldBolus != null)
				PreviewMoldVisibility = false;

			MoldFinalChanged?.Invoke();
			CurrentBolusChanged?.Invoke();
		}

		private void OnBolusRotate() {
			BolusRotate?.Invoke();
		}

		private void OnMoldScaleChanged() {
			BolusMold = null;
			GeneratePreviewMold();
			MoldPreviewChanged();
			MoldScaleChanged?.Invoke();
		}

		#endregion

		private AirChannelStore _airChannelStore;
		public BolusStore(AirChannelStore airChannelStore) {
			_airChannelStore = airChannelStore;
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

		public Model3DGroup BolusMoldModel3D {
			get {
				Model3DGroup meshes = new Model3DGroup();
				meshes.Children.Add(MeshToGeometry(BolusMold.MeshGeometry, Colors.Red, 0.6f, false));
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
			var backSkin = new DiffuseMaterial(new SolidColorBrush(color));
			backSkin.Brush.Opacity = 1.0f;
			result.BackMaterial = backSkin;

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
				return MeshToGeometry(mesh, Colors.Yellow, 0.8f);
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
			OnMoldPreviewChanged();
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
		private double _moldOffset = 4.0f;
		private int _moldResolution = 64;
		private float _moldOpacity = 0.4f;
		private Color _moldColor = Colors.Violet;
		private MoldTypes _moldType = MoldTypes.FLATTOP;
		private GeometryModel3D _moldMesh;
		private DMesh3 _previewMoldMesh;
		private double _lowest_airchannel_point;

		private double _moldScaleX = 0.98f;
		private double _moldScaleY = 0.98f;
		private double _moldScaleZ = 1.0f;

		private double _moldTranslateX = 0f;
		private double _moldTranslateY = 0f;
		private double _moldTranslateZ = 0f;

		public bool PreviewMoldVisibility { get => _previewMold; set { _previewMold = value; OnMoldPreviewChanged(); } }
		public double MoldOffset { get => _moldOffset;  set { _moldOffset = value; _previewMold = true;  OnMoldPreviewChanged(); } }
		public int MoldResolution { get => _moldResolution; set { _moldResolution = value; _previewMold = true; OnMoldPreviewChanged(); } }
		public float MoldOpacity { get => _moldOpacity; set { _moldOpacity = value; _previewMold = true; OnMoldPreviewChanged(); } }
		public Color MoldColor { get => _moldColor; set { _moldColor = value; _previewMold = true; OnMoldPreviewChanged(); } }
		public MoldTypes MoldType { get => _moldType; set { _moldType = value; _previewMold = true; OnMoldPreviewChanged(); } }
		public double LowestAirChannelPoint { set { _lowest_airchannel_point = value; OnMoldPreviewChanged(); } }

		public double MoldScaleX { get => _moldScaleX; set { _moldScaleX = value; OnMoldScaleChanged(); } }
		public double MoldScaleY { get => _moldScaleY; set { _moldScaleY = value; OnMoldScaleChanged(); } }
		public double MoldScaleZ { get => _moldScaleZ; set { _moldScaleZ = value; OnMoldScaleChanged(); } }

		public double MoldTranslateX { get => _moldTranslateX; set { _moldTranslateX = value; OnMoldScaleChanged(); } }
		public double MoldTranslateY { get => _moldTranslateY; set { _moldTranslateY = value; OnMoldScaleChanged(); } }
		public double MoldTranslateZ { get => _moldTranslateZ; set { _moldTranslateZ = value; OnMoldScaleChanged(); } }

		public GeometryModel3D MoldModel3D { get => _moldMesh; }

		private void GeneratePreviewMold() {
			if (CurrentBolus == null ||
			!_previewMold)
				return;

			BolusMold = null;

			DMesh3 rotated_mesh = ApplyBolusRotation(CurrentBolus.DMesh);
			DMesh3 result = new DMesh3();

			switch (_moldType) {

				case MoldTypes.BOX:
					result = BolusTools.GenerateBoxMold(rotated_mesh, MoldOffset, MoldResolution);
					break;
				case MoldTypes.CONTOURED:
					result = BolusTools.GenerateContourMold(rotated_mesh, _moldOffset, _moldResolution);
					break;
				case MoldTypes.FLATBOTTOM:
					result = BolusTools.GenerateFlattenedContourMold(rotated_mesh, _moldOffset, _moldResolution);
					break;
				case MoldTypes.FLATTOP:
					result = BolusTools.GenerateRaisedContourMold(rotated_mesh, _moldOffset, _moldResolution, _lowest_airchannel_point);
					break;
				default:
					result = BolusTools.GenerateMold(rotated_mesh, _moldOffset, _moldResolution);
					break;

			}

			MeshTransforms.Scale(result, _moldScaleX, _moldScaleY, _moldScaleZ);

			_previewMoldMesh = result;
			DiffuseMaterial material = new(new SolidColorBrush(_moldColor));
			material.Brush.Opacity = _moldOpacity;
			_moldMesh = new GeometryModel3D(BolusTools.DMeshToMeshGeometry(_previewMoldMesh), material) { BackMaterial = material };

		}

		public void GenerateMold(List<AirChannel> airChannels)  {
			if (CurrentBolus == null ||
			!_previewMold)
				return;

			if (_previewMoldMesh == null) //create the mold if none exists
				GeneratePreviewMold();

			if (_previewMoldMesh == null) //if the mold still doesn't exist, abort
				return;

			_moldBolus = null; //clears any previously generated mold

			BolusMold = new Bolus(BolusTools.GenerateFinalMold(_previewMoldMesh, ApplyBolusRotation(CurrentBolus.DMesh), airChannels));
		}

		#endregion
	}
}
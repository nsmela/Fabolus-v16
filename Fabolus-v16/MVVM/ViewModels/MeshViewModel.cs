using Fabolus_v16.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Fabolus_v16.Commands;
using System.Windows.Media;

namespace Fabolus_v16.MVVM.ViewModels {
	public class MeshViewModel : ViewModelBase {
		#region Bolus Mesh
		private readonly BolusStore _bolusStore;

		public Model3DGroup BolusMesh {
			get {
				if (_bolusStore != null)
					if (_bolusStore.CurrentBolus != null)
						return _bolusStore.BolusModel3D;
				return null;
			}
		}
		#endregion

		private bool _meshVisibility;
		public bool MeshVisibility {
			get => _meshVisibility;
			set {
				_meshVisibility = value;
				OnMeshVisibilityChanged();
			}
		}

		public MeshViewModel(BolusStore bolusStore, AirChannelStore airChannelStore) {
			_bolusStore = bolusStore;
			_airChannelStore = airChannelStore;
			_meshVisibility = true;

			_bolusStore.CurrentBolusChanged += OnCurrentBolusChanged;
			_airChannelStore.AirChannelsChanged += OnAirChannelsChanged;
			_airChannelStore.PreviewChannelChanged += OnPreviewAirChannelChanged;
		}

		public override void OnExit() {
			_bolusStore.CurrentBolusChanged -= OnCurrentBolusChanged;
			_airChannelStore.AirChannelsChanged -= OnAirChannelsChanged;
			_airChannelStore.PreviewChannelChanged -= OnPreviewAirChannelChanged;
		}

		#region Events and Property Changes
		private void OnCurrentBolusChanged() {
			OnPropertyChanged(nameof(BolusMesh));
		}
		private void OnAirChannelsChanged() {
			OnPropertyChanged(nameof(AirChannelMesh));
		}
		private void OnMeshVisibilityChanged() {
			OnPropertyChanged(nameof(MeshVisibility));
			OnPropertyChanged(nameof(AirChannelMesh));
		}
		private void OnPreviewAirChannelChanged() {
			OnPropertyChanged(nameof(PreviewAirChannelMesh));
		}

		#endregion

		#region Mouse Commands
		public event Action MouseDownAction;
		private void OnMouseDownAction() {
			MouseDownAction?.Invoke();
		}

		private MouseEventArgs _mouseDownArgs;
		public MouseEventArgs MouseDownEvent {
			get => _mouseDownArgs;
			set {
				_mouseDownArgs = value;
				OnMouseDownAction();
			}
		}

		public event Action MeshMouseDown;
		private void OnMeshMouseDown() {
			MeshMouseDown?.Invoke();
		}

		public event Action MeshMouseMove;
		private void OnMeshMouseMove() {
			MeshMouseMove?.Invoke();
		}

		#endregion

		#region Ray Tests
		private RayMeshGeometry3DHitTestResult _meshHit;
		public RayMeshGeometry3DHitTestResult MeshHit {
			get => _meshHit;
			set {
				_meshHit = value;

				if (_meshHit != null)
					OnMeshMouseDown();
			}
		}
		public RayMeshGeometry3DHitTestResult MeshHitMouseMove {
			set {
				_meshHit = value;
				if (_meshHit == null) {
					_airChannelStore.PreviewVisible = false;
					return;
				}

				_airChannelStore.PreviewPoint = _meshHit.PointHit;
				return;
			}
		}

		private RayMeshGeometry3DHitTestResult MouseToMesh(object sender, MouseButtonEventArgs e) {
			Point mousePosition = e.GetPosition((IInputElement)e.Source);

			_meshVisibility = false;
			OnPropertyChanged(nameof(MeshVisibility));
			OnPropertyChanged(nameof(AirChannelMesh));
			OnPropertyChanged(nameof(PreviewAirChannelMesh));
			HitTestResult hit = VisualTreeHelper.HitTest((HelixViewport3D)e.Source, mousePosition);
			_meshVisibility = true;
			OnPropertyChanged(nameof(MeshVisibility));
			OnPropertyChanged(nameof(AirChannelMesh));
			OnPropertyChanged(nameof(PreviewAirChannelMesh));
			return hit as RayMeshGeometry3DHitTestResult;
		}

		private RayMeshGeometry3DHitTestResult MouseToMesh(object sender, MouseEventArgs e) {
			Point mousePosition = e.GetPosition((IInputElement)e.Source);

			_meshVisibility = false;
			OnPropertyChanged(nameof(MeshVisibility));
			OnPropertyChanged(nameof(AirChannelMesh));
			OnPropertyChanged(nameof(PreviewAirChannelMesh));
			HitTestResult hit = VisualTreeHelper.HitTest((HelixViewport3D)e.Source, mousePosition);
			_meshVisibility = true;
			OnPropertyChanged(nameof(MeshVisibility));
			OnPropertyChanged(nameof(AirChannelMesh));
			OnPropertyChanged(nameof(PreviewAirChannelMesh));
			return hit as RayMeshGeometry3DHitTestResult;
		}
		#endregion

		#region airholes
		private AirChannelStore _airChannelStore;
		public AirChannelStore AirChannels { get => _airChannelStore; }

		public GeometryModel3D AirChannelMesh {
			get {
				if (AirChannels.Count < 1 || !_meshVisibility)
					return null;

				return AirChannels.AirChannelsMesh;
			}
		}
		public GeometryModel3D PreviewAirChannelMesh {
			get {
				if (!_meshVisibility ||
					!AirChannelVisibility)
					return null;

				return AirChannels.PreviewAirChannel;
			}
		}

		public bool AirChannelVisibility { get => _airChannelStore.Visibility; set => _airChannelStore.Visibility = value; }
		public void ClearAirChannels() => _airChannelStore.ClearAirChannels();

		#endregion

		#region Extending Access to the viewport to allow mouse events to be handled
		private RelayCommand _mouseUpCommand;
		public RelayCommand MouseUpCommand {
			get {
				if (_mouseUpCommand == null) _mouseUpCommand = new RelayCommand(param => MouseUp((MouseEventArgs)param));
				return _mouseUpCommand;
			}
			set { _mouseUpCommand = value; }
		}

		private RelayCommand _mouseDownCommand;
		public RelayCommand MouseDownCommand {
			get {
				if (_mouseDownCommand == null) _mouseDownCommand = new RelayCommand(param => MouseDown((object)param, (MouseButtonEventArgs)param));
				return _mouseDownCommand;
			}
			set { _mouseDownCommand = value; }
		}

		private RelayCommand _mouseMoveCommand;
		public RelayCommand MouseMoveCommand {
			get {
				if (_mouseMoveCommand == null) _mouseMoveCommand = new RelayCommand(param => MouseMove((object)param, (MouseEventArgs)param));
				return _mouseMoveCommand;
			}
			set { _mouseMoveCommand = value; }
		}

		//mouse commands
		private void MouseUp(MouseEventArgs e) {

		}
		private void MouseDown(object sender, MouseButtonEventArgs e) {
			if (_bolusStore.CurrentBolus == null)
				return;

			var result = MouseToMesh(sender, e);
			MeshHit = result;
		}
		private void MouseMove(object sender, MouseEventArgs e) {
			if (_bolusStore.CurrentBolus == null ||
				!AirChannelVisibility )
				return;

			if (e.RightButton == MouseButtonState.Pressed)
				return;

			var result = MouseToMesh(sender, e);
			MeshHitMouseMove = result;
		}

		#endregion
	}

	//for mouse interactions
	public class MouseBehaviour {
		#region Mouse Up
		public static readonly DependencyProperty MouseUpCommandProperty =
		DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseUpCommandChanged)));

		private static void MouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			FrameworkElement element = (FrameworkElement)d;

			element.MouseUp += new MouseButtonEventHandler(element_MouseUp);
		}

		static void element_MouseUp(object sender, MouseButtonEventArgs e) {
			FrameworkElement element = (FrameworkElement)sender;

			ICommand command = GetMouseUpCommand(element);

			command.Execute(e);
		}

		public static void SetMouseUpCommand(UIElement element, ICommand value) {
			element.SetValue(MouseUpCommandProperty, value);
		}

		public static ICommand GetMouseUpCommand(UIElement element) {
			return (ICommand)element.GetValue(MouseUpCommandProperty);
		}
		#endregion

		#region Mouse Down
		public static readonly DependencyProperty MouseDownCommandProperty =
		DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseDownCommandChanged)));

		private static void MouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			FrameworkElement element = (FrameworkElement)d;

			element.MouseDown += new MouseButtonEventHandler(element_MouseDown);
		}

		static void element_MouseDown(object sender, MouseButtonEventArgs e) {
			FrameworkElement element = (FrameworkElement)sender;

			ICommand command = GetMouseDownCommand(element);

			command.Execute(e);
		}

		public static void SetMouseDownCommand(UIElement element, ICommand value) {
			element.SetValue(MouseDownCommandProperty, value);
		}

		public static ICommand GetMouseDownCommand(UIElement element) {
			return (ICommand)element.GetValue(MouseDownCommandProperty);
		}
		#endregion

		#region Mouse Move
		public static readonly DependencyProperty MouseMoveCommandProperty =
			DependencyProperty.RegisterAttached("MouseMoveCommand", typeof(ICommand), typeof(MouseBehaviour), new FrameworkPropertyMetadata(new PropertyChangedCallback(MouseMoveCommandChanged)));

		private static void MouseMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			FrameworkElement element = (FrameworkElement)d;

			element.MouseMove += new MouseEventHandler(element_MouseMove);    //MouseButtonEventHandler(element_MouseMove);
		}

		static void element_MouseMove(object sender, MouseEventArgs e) {
			FrameworkElement element = (FrameworkElement)sender;

			ICommand command = GetMouseMoveCommand(element);

			command.Execute(e);
		}
		public static void SetMouseMoveCommand(UIElement element, ICommand value) {
			element.SetValue(MouseMoveCommandProperty, value);
		}

		public static ICommand GetMouseMoveCommand(UIElement element) {
			return (ICommand)element.GetValue(MouseMoveCommandProperty);
		}
		#endregion
	}
}

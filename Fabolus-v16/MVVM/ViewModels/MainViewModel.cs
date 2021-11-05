using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Fabolus_v16.Stores;
using Fabolus_v16.Commands;
using Fabolus_v16.MVVM.Models;

namespace Fabolus_v16.MVVM.ViewModels {
	public class MainViewModel : ViewModelBase {
		#region Navigation Store
		private NavigationStore _navigationStore;
		public NavigationStore MainNavigationStore { get => _navigationStore; }
		public ViewModelBase CurrentViewModel { get => _navigationStore.CurrentViewModel; }
		#endregion

		#region Bolus Store
		//bolus store
		private BolusStore _bolusStore;
		public BolusStore MainBolusStore { get => _bolusStore; }
		public String BolusVolume { get => _bolusStore.BolusVolume; }
		#endregion

		#region AirChannelStore
		private AirChannelStore _airChannelStore;
		public AirChannelStore MainAirChannelStore { get => _airChannelStore; }
		#endregion

		#region Views
		private string _currentViewTitle; //shows user which mode they've selected
		public string CurrentViewTitle {  get => _currentViewTitle; set { _currentViewTitle = value; OnPropertyChanged(nameof(CurrentViewTitle)); } }

		private MeshViewModel _meshViewModel;
		public MeshViewModel MeshVM { get => _meshViewModel; }

		#endregion

		#region Commands
		public ICommand LoadViewCommand { get; }
		public ICommand SmoothViewCommand { get; }
		public ICommand RotationViewCommand { get; }
		public ICommand AirChannelsViewCommand { get; }
		public ICommand MoldViewCommand { get; }
		public ICommand ExportMeshCommand { get; }
		public ICommand ImportSTLCommand { get; }
		#endregion

		public MainViewModel() {
			_bolusStore = new BolusStore();
			_airChannelStore = new AirChannelStore();

			_navigationStore = new NavigationStore();
			_navigationStore.CurrentViewModel = new LoadFileViewModel(this);

			_meshViewModel = new MeshViewModel(_bolusStore, _airChannelStore);

			_navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
			_bolusStore.CurrentBolusChanged += OnCurrentBolusChanged;

			//navigation commands
			LoadViewCommand = new NavigateCommand<LoadFileViewModel>(new Services.NavigationService<LoadFileViewModel>(MainNavigationStore, () => new LoadFileViewModel(this)));
			SmoothViewCommand = new NavigateCommand<SmoothViewModel>(new Services.NavigationService<SmoothViewModel>(MainNavigationStore, () => new SmoothViewModel(this, MainBolusStore)));
			RotationViewCommand = new NavigateCommand<RotateViewModel>(new Services.NavigationService<RotateViewModel>(MainNavigationStore, () => new RotateViewModel(this, MainBolusStore)));
			AirChannelsViewCommand = new NavigateCommand<AirChannelsViewModel>(new Services.NavigationService<AirChannelsViewModel>(MainNavigationStore, () => new AirChannelsViewModel(this)));
			MoldViewCommand = new NavigateCommand<MoldViewModel>(new Services.NavigationService<MoldViewModel>(MainNavigationStore, () => new MoldViewModel(this)));

			ImportSTLCommand = new LoadSTLCommand(this, _bolusStore);
			ExportMeshCommand = new ExportMeshCommand(_bolusStore);
		}

		private void OnCurrentViewModelChanged() {
			OnPropertyChanged(nameof(CurrentViewModel));
		}

		private void OnCurrentBolusChanged() {
			OnPropertyChanged(nameof(BolusVolume));
		}

		public override void OnExit() {
			_navigationStore.CurrentViewModelChanged -= OnCurrentViewModelChanged;
			_bolusStore.CurrentBolusChanged -= OnCurrentBolusChanged;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fabolus_v16.MVVM.ViewModels;
using Fabolus_v16.Stores;

namespace Fabolus_v16.Services {
	class NavigationService<TViewModel> where TViewModel : ViewModelBase {
		private readonly NavigationStore _navigationStore;
		private readonly Func<TViewModel> _createViewModel;

		public NavigationService(NavigationStore navigationStore, Func<TViewModel> createViewModel) {
			_navigationStore = navigationStore;
			_createViewModel = createViewModel;
		}

		public void Navigate() {
			_navigationStore.CurrentViewModel.OnExit();
			_navigationStore.CurrentViewModel = _createViewModel();
		}
	}

}

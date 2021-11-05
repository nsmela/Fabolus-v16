using Fabolus_v16.Stores;
using Fabolus_v16.MVVM.ViewModels;
using System;
using Fabolus_v16.Services;

namespace Fabolus_v16.Commands {
	class NavigateCommand<TViewModel> : CommandBase where TViewModel : ViewModelBase {
		private readonly NavigationService<TViewModel> _navigationService;

		public NavigateCommand(NavigationService<TViewModel> navigationService) {
			_navigationService = navigationService;
		}

		public override void Execute(object parameter) {
			_navigationService.Navigate();
		}
	}
}

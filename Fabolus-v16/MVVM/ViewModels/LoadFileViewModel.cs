using Fabolus_v16.Stores;
using Fabolus_v16.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using Fabolus_v16.MVVM.Models;
using System.Windows;

namespace Fabolus_v16.MVVM.ViewModels {
	class LoadFileViewModel : ViewModelBase {
		private readonly BolusStore _bolusStore;
		private MainViewModel _mainViewModel;
		private bool _isBusy = false;

		public IAsyncCommand LoadFileCommand { get; }

		public LoadFileViewModel(MainViewModel mainViewModel) {
			_bolusStore = mainViewModel.MainBolusStore;
			_mainViewModel = mainViewModel;
			_mainViewModel.CurrentViewTitle = "load file";

			LoadFileCommand = new AsyncCommand(ExecuteLoadSTLAsync, CanExecuteLoadFile);

			//clears any bolus models displayed
			_bolusStore.PreviewMoldVisibility = false;
			_bolusStore.CurrentBolus = null;

			_bolusStore.ClearBolusTransforms();
		}

		private async Task ExecuteLoadSTLAsync() {
			if (_isBusy) return;

			_isBusy = true;
			var LoadCommand = new LoadSTLAsyncCommand(_mainViewModel, _bolusStore);
			await LoadCommand.ExecuteAsync();
			_isBusy = false;
        }

		private bool CanExecuteLoadFile() => !_isBusy;

	}
	
}

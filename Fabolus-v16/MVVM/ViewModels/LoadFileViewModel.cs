using Fabolus_v16.Stores;
using Fabolus_v16.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Fabolus_v16.MVVM.ViewModels {
	class LoadFileViewModel : ViewModelBase {
		private readonly BolusStore _bolusStore;

		public ICommand LoadFileCommand { get; }

		public LoadFileViewModel(MainViewModel mainViewModel) {
			_bolusStore = mainViewModel.MainBolusStore;
			mainViewModel.CurrentViewTitle = "load file";

			LoadFileCommand = new LoadSTLCommand(mainViewModel, _bolusStore);

			//clears any bolus models displayed
			_bolusStore.ShowMold = false;
			_bolusStore.CurrentBolus = null;
			_bolusStore.ClearMoldMesh();
			_bolusStore.ClearBolusTransforms();
		}
	}
}

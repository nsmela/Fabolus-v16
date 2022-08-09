using Fabolus_v16.MVVM.Models;
using Fabolus_v16.MVVM.ViewModels;
using Fabolus_v16.Stores;
using g3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Fabolus_v16.Commands {
    public class LoadSTLAsyncCommand : IAsyncCommand {
		private readonly BolusStore _bolusStore;
		private readonly MainViewModel _mainViewModel;

		public LoadSTLAsyncCommand(MainViewModel mainViewModel, BolusStore bolusstore) {
			_bolusStore = bolusstore;
			_mainViewModel = mainViewModel;
		}

        public event EventHandler CanExecuteChanged;

        public bool CanExecute() => true;

		public bool CanExecute(object parameter) => CanExecute();

        public void Execute(object parameter) => ExecuteAsync();

        public async Task ExecuteAsync() {
            //open file dialog box
            Microsoft.Win32.OpenFileDialog openFile = new() {
				Filter = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*",
				Multiselect = false
			};

			//if successful, create mesh
			if (openFile.ShowDialog() != true)
				return;

			var filepath = openFile.FileName;

			if (!File.Exists(filepath)) {
				System.Windows.MessageBox.Show("Unable to find: " + filepath);
				return;
			}

			//because async can only access info in it's thread, has to generate a bolus then configure it
			//var mesh = await Bolus.ReadFileAsync(filepath);
			var newBolus = await Bolus.ReadBolusAsync(filepath);
			Action createBolus = new Action(() => { CreateBolus(newBolus); });

            _mainViewModel.MainDispatcher.BeginInvoke(createBolus, DispatcherPriority.ContextIdle);

			if (_mainViewModel != null) {
				_mainViewModel.SmoothViewCommand.Execute(true);
			}
		}

		private void CreateBolus(Bolus bolus) => _bolusStore.BolusRaw = bolus;
    }
}

using Fabolus_v16.Stores;
using Fabolus_v16.MVVM.ViewModels;
using System;
using Microsoft.Win32;
using System.IO;
using Fabolus_v16.Services;
using System.Threading.Tasks;
using System.Threading;
using Fabolus_v16.MVVM.Models;
using System.Windows.Threading;

namespace Fabolus_v16.Commands {
	class LoadSTLCommand : CommandBase {
		private readonly BolusStore _bolusStore;
		private readonly MainViewModel _mainViewModel;

		public LoadSTLCommand(BolusStore bolusstore) {
			_bolusStore = bolusstore;
		}

		public LoadSTLCommand(MainViewModel mainViewModel, BolusStore bolusstore) {
			_bolusStore = bolusstore;
			_mainViewModel = mainViewModel;
		}

		public override void Execute(object parameter) {
			//open file dialog box
			OpenFileDialog openFile = new() {
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

			_bolusStore.BolusRaw = new Bolus(filepath);

			if (_mainViewModel != null) {
				_mainViewModel.SmoothViewCommand.Execute(true);
			}
		}

	}
}

using g3;
using Microsoft.Win32;
using Fabolus_v16.Stores;

namespace Fabolus_v16.Commands {
	public class ExportMeshCommand : CommandBase {
		private readonly BolusStore _bolusStore;

		public ExportMeshCommand(BolusStore bolusStore) {
			_bolusStore = bolusStore;
		}

		public override void Execute(object parameter) {
			if (_bolusStore.CurrentBolus == null)
				return;

			SaveFileDialog saveFile = new() {
				Filter = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*"
			};

			//if successful, create mesh
			if (saveFile.ShowDialog() != true)
				return;

			var filepath = saveFile.FileName;

			var mesh = new DMesh3();
			if (_bolusStore.BolusMold != null)
				mesh = _bolusStore.BolusMold.DMesh;
			else
				mesh = _bolusStore.CurrentBolus.DMesh;

			StandardMeshWriter.WriteMesh(filepath, mesh, WriteOptions.Defaults);
		}
	}
}

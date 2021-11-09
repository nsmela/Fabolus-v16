using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;

namespace Fabolus_v16 {
	public static partial class BolusTools {
		static DMesh3 OffsetMesh(DMesh3 sourceMesh, double offset, int numcells) {
			BoundedImplicitFunction3d meshImplicit = meshToImplicitF(sourceMesh, numcells, offset);
			return generatMeshF(new ImplicitOffset3d() { A = meshImplicit, Offset = offset }, numcells);
		}

	}
}

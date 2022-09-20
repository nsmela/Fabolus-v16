using g3;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus_v16 {
	public static partial class BolusTools {
		public static DMesh3 OrientationCentre(DMesh3 mesh) {
            double x = mesh.CachedBounds.Center.x * -1;
            double y = mesh.CachedBounds.Center.y * -1;
            double z = mesh.CachedBounds.Center.z * -1;
            MeshTransforms.Translate(mesh, x, y, z);
			Log.Information($"translated mesh x: {x.ToString("0.00")} y: {y.ToString("0.00")} z: {z.ToString("0.00")}");
            return mesh;
        }

		static void ScaleMesh(DMesh3 mesh, double scaleX, double scaleY, double scaleZ) {
			Vector3d scale = new Vector3d(
				scaleX,
				scaleY,
				scaleZ);
			MeshTransforms.Scale(mesh, scale, Vector3d.Zero);
		}
	}
}

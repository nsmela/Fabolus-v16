using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using g3;

namespace Fabolus_v16 {
	public static partial class BolusTools {

        public static double CalculateVolume(DMesh3 mesh) {
            double volume = 0f;

            if (mesh != null) {
                Point3D p1, p2, p3;
                foreach (var triangle in mesh.Triangles()) {
                    var v = new Vector3d(mesh.GetVertex(triangle.a));
                    p1 = new Point3D(v.x, v.y, v.z);

                    v = new Vector3d(mesh.GetVertex(triangle.b));
                    p2 = new Point3D(v.x, v.y, v.z);

                    v = new Vector3d(mesh.GetVertex(triangle.c));
                    p3 = new Point3D(v.x, v.y, v.z);

                    volume += SignedVolumeOfTriangle(p1, p2, p3);
                }

            }

            return volume / 1000;
        }

        public static string CalculateVolumeText(DMesh3 mesh) {
            var volume = CalculateVolume(mesh);
            return volume.ToString("0.00");
        }

        /// <summary>
        /// calculates volume of a triangle. signed so that negative volumes exist, easing the calculation
        /// </summary>
        private static double SignedVolumeOfTriangle(Point3D p1, Point3D p2, Point3D p3) {
            var v321 = p3.X * p2.Y * p1.Z;
            var v231 = p2.X * p3.Y * p1.Z;
            var v312 = p3.X * p1.Y * p2.Z;
            var v132 = p1.X * p3.Y * p2.Z;
            var v213 = p2.X * p1.Y * p3.Z;
            var v123 = p1.X * p2.Y * p3.Z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }
    }
}

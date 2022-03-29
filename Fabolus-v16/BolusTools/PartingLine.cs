using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus_v16 {
	public static partial class BolusTools {

		public static MeshGeometry3D TriangleBand(MeshGeometry3D mesh, List<Vector3D> triangleNormals, Transform3D transform) {
			double angularTolerance = 80.0f; //degrees
			double lowAngularTolerance = 90 - angularTolerance / 2;
			double highAngularTolerance = 90 + angularTolerance / 2;
			double distanceTolerance = 10.0f; //10 mm
											 //area thresthold

			Vector3D referenceYAngle = new Vector3D(0, 1, 0);
			double angleBetween;
			bool visible = false;
			var meshBuilder = new MeshBuilder(true);
			for (int i = 0; i < triangleNormals.Count - 1; i++) {
				//see if the triangle falls within the range
				angleBetween = Vector3D.AngleBetween(transform.Transform(triangleNormals[i]), referenceYAngle);
				if (angleBetween > lowAngularTolerance && angleBetween < highAngularTolerance) {
					visible = true;
				} 

				//if the triangle is flagged as an overhang
				if (visible) {
					int triangleIndex = i * 3;

					Point3D p0 = mesh.Positions[mesh.TriangleIndices[triangleIndex]];
					Point3D p1 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 1]];
					Point3D p2 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 2]];
					meshBuilder.AddTriangle(p0, p1, p2);
					visible = false;
				}
			}

			return meshBuilder.ToMesh();
		}
	}
}

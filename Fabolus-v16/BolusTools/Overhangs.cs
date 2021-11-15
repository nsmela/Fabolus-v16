using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Fabolus_v16 {
	public static partial class BolusTools {
		public static MeshGeometry3D GetOverhangs(MeshGeometry3D mesh, List<Vector3D> triangleNormals, Transform3D transform, double angleLimit) {
			Vector3D reference = new Vector3D(0, 0, 1);

			var meshBuilder = new MeshBuilder(true);
			for (int i = 0; i < triangleNormals.Count - 1; i++) {
				//calculate triangle normal
				double difference = Vector3D.AngleBetween(transform.Transform(triangleNormals[i]), reference);

				//if the triangle is flagged as an overhang
				if (difference < angleLimit) {
					int triangleIndex = i * 3;

					Point3D p0 = mesh.Positions[mesh.TriangleIndices[triangleIndex]];
					Point3D p1 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 1]];
					Point3D p2 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 2]];
					meshBuilder.AddTriangle(p0, p1, p2);
				}
			}
			return meshBuilder.ToMesh();
		}

		public static List<Vector3D> CalculateSurfaceNormals(MeshGeometry3D mesh) {
			if (mesh == null) return null;

			List<Vector3D> surfaceNormals = new();

			//calculate for each triangle
			for (int triangle = 0; triangle < mesh.TriangleIndices.Count; triangle += 3) {
				//get the triangle's normal
				int i0 = mesh.TriangleIndices[triangle];
				int i1 = mesh.TriangleIndices[triangle + 1];
				int i2 = mesh.TriangleIndices[triangle + 2];

				Point3D p0 = mesh.Positions[i0];
				Point3D p1 = mesh.Positions[i1];
				Point3D p2 = mesh.Positions[i2];

				surfaceNormals.Add(CalculateSurfaceNormal(p0, p1, p2));
			}

			return surfaceNormals;
		}

		private static Vector3D CalculateSurfaceNormal(Point3D p1, Point3D p2, Point3D p3) {
			Vector3D v1 = new Vector3D(0, 0, 0);             // Vector 1 (x,y,z) & Vector 2 (x,y,z)
			Vector3D v2 = new Vector3D(0, 0, 0);
			Vector3D normal = new Vector3D(0, 0, 0);

			// Finds The Vector Between 2 Points By Subtracting
			// The x,y,z Coordinates From One Point To Another.

			// Calculate The Vector From Point 2 To Point 1
			v1.X = p1.X - p2.X;                  // Vector 1.x=Vertex[0].x-Vertex[1].x
			v1.Y = p1.Y - p2.Y;                  // Vector 1.y=Vertex[0].y-Vertex[1].y
			v1.Z = p1.Z - p2.Z;                  // Vector 1.z=Vertex[0].y-Vertex[1].z
												 // Calculate The Vector From Point 3 To Point 2
			v2.X = p2.X - p3.X;                  // Vector 1.x=Vertex[0].x-Vertex[1].x
			v2.Y = p2.Y - p3.Y;                  // Vector 1.y=Vertex[0].y-Vertex[1].y
			v2.Z = p2.Z - p3.Z;                  // Vector 1.z=Vertex[0].y-Vertex[1].z

			// Compute The Cross Product To Give Us A Surface Normal
			normal.X = v1.Y * v2.Z - v1.Z * v2.Y;   // Cross Product For Y - Z
			normal.Y = v1.Z * v2.X - v1.X * v2.Z;   // Cross Product For X - Z
			normal.Z = v1.X * v2.Y - v1.Y * v2.X;   // Cross Product For X - Y

			normal.Normalize();

			return normal;
		}


	}
}

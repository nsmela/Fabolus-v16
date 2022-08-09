using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using g3;

namespace Fabolus_v16.MVVM.Models {
	public class Bolus {
		private DMesh3 _mesh;
		private MeshGeometry3D _meshGeometry;

		public MeshGeometry3D MeshGeometry { get => _meshGeometry; }
		public DMesh3 DMesh { get => _mesh; }

		public Bolus(string filepath) {
			_mesh = OpenFile(filepath);

			if (_mesh != null) {
				_mesh = BolusTools.OrientationCentre(_mesh);
				_meshGeometry = BolusTools.DMeshToMeshGeometry(_mesh);
				SetNormals();
			}
		}

		public Bolus(MeshGeometry3D mesh) {
			_meshGeometry = mesh;
			_mesh = BolusTools.MeshGeometryToDMesh(_meshGeometry);
			SetNormals();
		}

		public Bolus(DMesh3 mesh) {
			_mesh = mesh;
			_meshGeometry = BolusTools.DMeshToMeshGeometry(_mesh);
			SetNormals();
		}

		private DMesh3 OpenFile(string filepath) {
			return StandardMeshReader.ReadMesh(filepath);
		}

		public async static Task<DMesh3> ReadFileAsync(string filepath) {
			return await Task<DMesh3>.Run(() => StandardMeshReader.ReadMesh(filepath));
		}

		public async static Task<Bolus> ReadBolusAsync(string filepath) {
			return await Task<Bolus>.Run( ()=>  new Bolus(filepath));
		}

		#region Overhangs Calculation
		public MeshGeometry3D OverhangMesh(Transform3D transform, double angle) {
			return BolusTools.GetOverhangs(_meshGeometry, _triangleNormals, transform, angle);
		}

		#endregion

		#region Triangle Normals
		//triangle normals are calculated whenever the mesh is changed
		//they are used to determine overhangs
		//this is faster than calculating the tirangle normals each time a rotation is applied
		//instead, the normals are calculated once and this list is used for each overhang calculation instead
		private List<Vector3D> _triangleNormals; 

		private void SetNormals() {
			_triangleNormals = new List<Vector3D>();
			_triangleNormals = BolusTools.CalculateSurfaceNormals(_meshGeometry);
		}

		public static List<Vector3D> CalculateSurfaceNormals(MeshGeometry3D mesh) {
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
		#endregion

	}
}

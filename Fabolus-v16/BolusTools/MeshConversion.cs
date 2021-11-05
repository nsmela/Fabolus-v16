using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus_v16 {
    public static partial class BolusTools {
        public static MeshGeometry3D DMeshToMeshGeometry(DMesh3 value) {
            if (value != null) {
                //compacting the DMesh to the indices are true
                MeshGeometry3D mesh = new();

                //calculate positions
                var vertices = value.Vertices();
                foreach (var vert in vertices)
                    mesh.Positions.Add(new Point3D(vert.x, vert.y, vert.z));

                //calculate faces
                var vID = value.VertexIndices().ToArray();
                var faces = value.Triangles();
                foreach (Index3i f in faces) {
                    mesh.TriangleIndices.Add(Array.IndexOf(vID, f.a));
                    mesh.TriangleIndices.Add(Array.IndexOf(vID, f.b));
                    mesh.TriangleIndices.Add(Array.IndexOf(vID, f.c));
                }

                return mesh;
            } else
                return null;
        }

        public static DMesh3 MeshGeometryToDMesh(MeshGeometry3D mesh) {
            List<Vector3d> vertices = new();
            foreach (Point3D point in mesh.Positions)
                vertices.Add(new Vector3d(point.X, point.Y, point.Z));

            List<Vector3f> normals = new();
            foreach (Point3D normal in mesh.Normals)
                normals.Add(new Vector3f(normal.X, normal.Y, normal.Z));

            if (normals.Count == 0)
                normals = null;

            List<Index3i> triangles = new();
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                triangles.Add(new Index3i(mesh.TriangleIndices[i], mesh.TriangleIndices[i + 1], mesh.TriangleIndices[i + 2]));

            //converting the meshes to use Implicit Surface Modeling
            return DMesh3Builder.Build(vertices, triangles, normals);
        }

        public static DMesh3 MeshGeometryToDMesh(MeshGeometry3D mesh, Matrix3D transform) {
            //applying the transform
            Point3DCollection positions = new();
            foreach (Point3D p in mesh.Positions)
                positions.Add(transform.Transform(p));

            List<Vector3d> vertices = new();
            foreach (Point3D point in positions)
                vertices.Add(new Vector3d(point.X, point.Y, point.Z));

            List<Vector3f> normals = new();
            foreach (Point3D normal in mesh.Normals)
                normals.Add(new Vector3f(normal.X, normal.Y, normal.Z));

            if (normals.Count == 0)
                normals = null;

            List<Index3i> triangles = new();
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                triangles.Add(new Index3i(mesh.TriangleIndices[i], mesh.TriangleIndices[i + 1], mesh.TriangleIndices[i + 2]));

            //converting the meshes to use Implicit Surface Modeling
            return DMesh3Builder.Build(vertices, triangles, normals);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Fabolus_v16.MVVM.Models;
using g3;

namespace Fabolus_v16 {
	public static partial class BolusTools {
        public static MeshGeometry3D Smooth(double edgeLength, double smoothSpeed, double iterations, double cells, Bolus bolus) {
            DMesh3 mesh = bolus.DMesh;
            return Smooth(edgeLength, smoothSpeed, iterations, cells, mesh);
		}

        /// <summary>
        /// uses marching cubes to help smooth the mesh after using the remesher
        /// experimental
        /// </summary>
        /// <param name="edgeLength"></param>
        /// <param name="smoothSpeed"></param>
        /// <param name="iterations"></param>
        /// <param name="cells"></param>
        public static MeshGeometry3D Smooth(double edgeLength, double smoothSpeed, double iterations, double cells, DMesh3 _mesh) {
            //Use the Remesher class to do a basic remeshing
            DMesh3 mesh = new DMesh3(_mesh);
            Remesher r = new Remesher(mesh);
            r.PreventNormalFlips = true;
            r.SetTargetEdgeLength(edgeLength);
            r.SmoothSpeedT = smoothSpeed;
            r.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));
            for (int k = 0; k < iterations; k++)
                r.BasicRemeshPass();

            //marching cubes
            int num_cells = (int)cells;
            DMesh3 _smoothMesh = new();
            if (cells > 0) {
                double cell_size = mesh.CachedBounds.MaxDim / num_cells;

                MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size);
                sdf.Compute();

                var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);

                MarchingCubes c = new MarchingCubes();
                c.Implicit = iso;
                c.Bounds = mesh.CachedBounds;
                c.CubeSize = c.Bounds.MaxDim / cells;
                c.Bounds.Expand(3 * c.CubeSize);

                c.Generate();

                _smoothMesh = c.Mesh;
            }

            if (_smoothMesh == null)
                return null;

            return BolusTools.DMeshToMeshGeometry(_smoothMesh);

        }
    }
}

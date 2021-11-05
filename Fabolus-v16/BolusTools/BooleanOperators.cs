using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus_v16 {
	public static partial class BolusTools {
        public static DMesh3 BooleanUnion(DMesh3 mesh1, DMesh3 mesh2) {
            BoundedImplicitFunction3d meshA = meshToImplicitF(mesh1, 128, 0.2f);
            BoundedImplicitFunction3d meshB = meshToImplicitF(mesh2, 128, 0.2f);

            //take the difference of the bolus mesh minus the tools
            var mesh = new ImplicitUnion3d() { A = meshA, B = meshB };

            //calculate the boolean mesh
            MarchingCubes c = new MarchingCubes();
            c.Implicit = mesh;
            c.RootMode = MarchingCubes.RootfindingModes.LerpSteps;
            c.RootModeSteps = 5;
            c.Bounds = mesh.Bounds();
            c.CubeSize = c.Bounds.MaxDim / 96;
            c.Bounds.Expand(3 * c.CubeSize);
            c.Generate();
            MeshNormals.QuickCompute(c.Mesh);

            //int triangleCount = c.Mesh.TriangleCount / 2;
            //Reducer r = new Reducer(c.Mesh);
            //r.ReduceToTriangleCount(triangleCount);
            return c.Mesh;
        }

        public static DMesh3 BooleanSubtraction(DMesh3 mesh1, DMesh3 mesh2) {
            BoundedImplicitFunction3d meshA = meshToImplicitF(mesh1, 128, 0.2f);
            BoundedImplicitFunction3d meshB = meshToImplicitF(mesh2, 128, 0.2f);

            //take the difference of the bolus mesh minus the tools
            ImplicitDifference3d mesh = new ImplicitDifference3d() { A = meshA, B = meshB };

            //calculate the boolean mesh
            MarchingCubes c = new MarchingCubes();
            c.Implicit = mesh;
            c.RootMode = MarchingCubes.RootfindingModes.LerpSteps;
            c.RootModeSteps = 5;
            c.Bounds = mesh.Bounds();
            c.CubeSize = c.Bounds.MaxDim / 128;
            c.Bounds.Expand(3 * c.CubeSize);
            c.Generate();
            MeshNormals.QuickCompute(c.Mesh);

            //int triangleCount = c.Mesh.TriangleCount / 2;
            //Reducer r = new Reducer(c.Mesh);
            //r.ReduceToTriangleCount(triangleCount);
            return c.Mesh;
        }


        // meshToImplicitF() generates a narrow-band distance-field and
        // returns it as an implicit surface, that can be combined with other implicits                       
        private static Func<DMesh3, int, double, BoundedImplicitFunction3d> meshToImplicitF = (meshIn, numcells, max_offset) => {
            double meshCellsize = meshIn.CachedBounds.MaxDim / numcells;
            MeshSignedDistanceGrid levelSet = new MeshSignedDistanceGrid(meshIn, meshCellsize);
            levelSet.ExactBandWidth = (int)(max_offset / meshCellsize) + 1;
            levelSet.Compute();
            return new DenseGridTrilinearImplicit(levelSet.Grid, levelSet.GridOrigin, levelSet.CellSize);
        };

        // generateMeshF() meshes the input implicit function at
        // the given cell resolution, and writes out the resulting mesh    
        private static DMesh3 generatMeshF(BoundedImplicitFunction3d root, int numcells) {
            MarchingCubes c = new MarchingCubes();
            c.Implicit = root;
            c.RootMode = MarchingCubes.RootfindingModes.LerpSteps;      // cube-edge convergence method
            c.RootModeSteps = 5;                                        // number of iterations
            c.Bounds = root.Bounds();
            c.CubeSize = c.Bounds.MaxDim / numcells;
            c.Bounds.Expand(3 * c.CubeSize);                            // leave a buffer of cells
            c.Generate();
            MeshNormals.QuickCompute(c.Mesh);                           // generate normals
            return c.Mesh;   // write mesh
        }

    }
}

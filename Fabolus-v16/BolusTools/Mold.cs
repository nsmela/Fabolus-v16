using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Fabolus_v16.MVVM.Models;
using g3;
using HelixToolkit.Wpf;

namespace Fabolus_v16 {
    public static partial class BolusTools {
        /* A preview Mold requires:
         * -offset mesh
         * -voxilized that offset
         * -smooth that offset
         * 
         * A generated Mold also includes a boolean subtraction
         * 
         * Mold types depend on how the mold is voxilized
         * box: for each x,y: return true if theres at least one true on the z-plane
         * flatbottom: going top-to-bottom, save the previous layer's shape
         * flattop: going bottom-to-top, save the previous layer's shape and start at a specific height
         * contour: do not voxilize
         */
        public static DMesh3 GenerateMold(DMesh3 mesh, double offset, double airholeLevel = -1000) {
            //generate offset mesh
            var offsetMold = OffsetMesh(mesh, offset);

            return VoxilizedMold(offsetMold, airholeLevel);
        }

        static DMesh3 VoxilizedMold(DMesh3 mesh, double airholeLevel) {
            int numcells = 64;

            int airhole_z_height = Convert.ToInt32((mesh.CachedBounds.Height / numcells) * airholeLevel);

            DMesh3 resultMesh = new DMesh3(Voxelize(mesh, numcells, airhole_z_height));
            resultMesh = new DMesh3(MarchingCubesSmoothing(resultMesh, numcells));

            //scale the mesh to the original's size
            double scale_x = mesh.CachedBounds.Width / (resultMesh.CachedBounds.Width - 2);
            double scale_y = mesh.CachedBounds.Depth / (resultMesh.CachedBounds.Depth - 2);
            double scale_z = mesh.CachedBounds.Height / resultMesh.CachedBounds.Height;
            MeshTransforms.Scale(resultMesh, scale_x, scale_y, scale_z);

            //positioning the mesh ontop of the old one
            double x = mesh.CachedBounds.Center.x - resultMesh.CachedBounds.Center.x;
            double y = mesh.CachedBounds.Center.y - resultMesh.CachedBounds.Center.y;
            double z = mesh.CachedBounds.Center.z - resultMesh.CachedBounds.Center.z;
            MeshTransforms.Translate(resultMesh, x, y, z);

            return resultMesh;
        }

        static DMesh3 Voxelize(DMesh3 mesh, int numcells, int airhole_z_height) {
            //create voxel mesh
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, autoBuild: true);
            AxisAlignedBox3d bounds = mesh.CachedBounds;

            double cellsize = bounds.MaxDim / numcells;
            ShiftGridIndexer3 indexer = new ShiftGridIndexer3(bounds.Min, cellsize);

            Bitmap3 bmp = new Bitmap3(new Vector3i(numcells, numcells, numcells));
            foreach (Vector3i idx in bmp.Indices()) {
                Vector3d v = indexer.FromGrid(idx);
                bmp.Set(idx, spatial.IsInside(v));
            }

            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = BitmapExtendedToFloor(bmp, airhole_z_height);
            voxGen.Generate();
            return voxGen.Meshes[0];
        }

		static Bitmap3 BitmapExtendedToFloor(Bitmap3 bmp, int z_height = 0) {
			int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];
			int zTop = bmp.Dimensions.z - 1;

			//if an airhole is too low, this will extend the mesh from the lowest airhole up to the top.
			//the 3D print will be easier to fill
			if (z_height > 0) {
				for (int x = 0; x < bmp.Dimensions.x; x++) {
					for (int y = 0; y < bmp.Dimensions.y; y++) {
						for (int z = z_height; z < bmp.Dimensions.z; z++) {
							bool value = bmp.Get(new Vector3i(x, y, z - 1));
							bmp.Set(new Vector3i(x, y, z), value);
						}
					}
				}
			}

			return bmp;
			/*
			//check the very top for filled voxels (true or false)
			//-1 if nothing is above, otherwise number is how far from filled voxel above this one
			//to be used later one for more robust calculations
			for(int x = 0; x < bmp.Dimensions.x; x++) {
				for (int y = 0; y < bmp.Dimensions.y; y++) {
					if (bmp.Get(new Vector3i(x, y, zTop))) grid[x, y, zTop] = 0;
					else grid[x, y, zTop] = -1;
				}
			}

			//cycles from top to bottom
			//filled is original is filled
			//if not, counts how far from a filled cell above itself
			//-1 if whole cell stack is empty so far
			for (int z = zTop-1; z >= 0; z--) {
				for (int x = 0; x < bmp.Dimensions.x; x++) {
					for (int y = 0; y < bmp.Dimensions.y; y++) {
						Vector3i cell = new Vector3i(x, y, z);
						bool value = bmp.Get(cell);

						if (value) grid[x, y, z] = 0; //actively filled
						else if (grid[x, y, z + 1] > -1) grid[x, y, z] = grid[x, y, z + 1] + 1; //not filled, but how far from a filled cell
						else grid[x, y, z] = -1; //not filled, no filled cells above
					}
				}
			}
			
			//pass the grid results over to a new bitmap
			Bitmap3 result= new Bitmap3(bmp.Dimensions);
			for (int x = 0; x < bmp.Dimensions.x; x++) {
				for (int y = 0; y < bmp.Dimensions.y; y++) {
					for (int z = 0; z < bmp.Dimensions.z; z++) {
						bool value = grid[x, y, z] > -1;
						result.Set(new Vector3i(x, y, z), value);
					}
				}
			}

			return result;
			*/
		}

		static DMesh3 MarchingCubesSmoothing(DMesh3 mesh, int numcells) {
			double cell_size = mesh.CachedBounds.MaxDim / (numcells / 4);

			MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size);
			sdf.Compute();

			var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);

			MarchingCubes c = new MarchingCubes();
			c.Implicit = iso;
			c.Bounds = mesh.CachedBounds;
			c.CubeSize = c.Bounds.MaxDim / 32;
			c.Bounds.Expand(3 * c.CubeSize);

			c.Generate();
			return c.Mesh;
		}
	}
}

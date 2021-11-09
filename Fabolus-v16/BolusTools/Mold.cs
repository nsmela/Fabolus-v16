using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using Fabolus_v16.MVVM.Models;
using g3;
using gs;
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
		public static DMesh3 GenerateMold(DMesh3 mesh, double offset, int resolution = 64, double airholeLevel = -1000) {
			//generate offset mesh
			var offsetMold = OffsetMesh(mesh, offset, resolution);

			return VoxilizedMold(offsetMold, airholeLevel, resolution);
		}

		public static DMesh3 GenerateContourMold(DMesh3 mesh, double offset, int resolution = 64) {
			//create an inflated mold to contain the original
			var offsetMold = OffsetMesh(mesh, offset, resolution);

			//bitmap use to define how the mesh voxilization goes
			Bitmap3 bmp = MeshBitmap(offsetMold, resolution);

			//turn it into a voxilized mesh
			VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
			voxGen.Voxels = bmp;
			voxGen.Generate();
			var result = new DMesh3(MarchingCubesSmoothing(voxGen.Meshes[0], resolution));
			CentreMesh(result, offsetMold);
			ScaleMesh(result, mesh, offset);

			return result;
		}

		public static DMesh3 GenerateBoxMold(DMesh3 mesh, double offset, int resolution = 64) {
			//create an inflated mold to contain the original
			var offsetMold = OffsetMesh(mesh, offset, resolution);

			//bitmap use to define how the mesh voxilization goes
			Bitmap3 bmp = MeshBitmap(offsetMold, resolution);

			//a contour mold doesn't modify the mesh any further.

			//turn it into a voxilized mesh
			VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
			voxGen.Voxels = BitmapBox(bmp);
			voxGen.Generate();
			var result = new DMesh3(MarchingCubesSmoothing(voxGen.Meshes[0], resolution));
			CentreMesh(result, offsetMold);

			return result;
		}

		public static DMesh3 GenerateFlattenedContourMold(DMesh3 mesh, double offset, int resolution = 64) {
			//create an inflated mold to contain the original
			var offsetMold = OffsetMesh(mesh, offset, resolution);

			//bitmap use to define how the mesh voxilization goes
			Bitmap3 bmp = MeshBitmap(offsetMold, resolution);

			//turn it into a voxilized mesh
			VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
			voxGen.Voxels = BitmapExtendedToFloor(bmp);
			voxGen.Generate();
			var result = new DMesh3(MarchingCubesSmoothing(voxGen.Meshes[0], resolution));
			CentreMesh(result, offsetMold);

			return result;
		}

		public static DMesh3 GenerateRaisedContourMold(DMesh3 mesh, double offset, int resolution, double airhole_height) {
			//create an inflated mold to contain the original
			var offsetMold = OffsetMesh(mesh, offset, resolution);

			//bitmap use to define how the mesh voxilization goes
			Bitmap3 bmp = MeshBitmap(offsetMold, resolution);

			//calculate the z height for the lowest airhole
			if (airhole_height < -900) //if there's no airholes
				airhole_height = offsetMold.CachedBounds.Max.z;

			double distance_from_mesh_bottom = Math.Abs( airhole_height - offsetMold.CachedBounds.Min.z);
			double distance_per_cell = (offsetMold.CachedBounds.Max.z - offsetMold.CachedBounds.Min.z) / resolution;
			int z_height = Math.Clamp((int)Math.Round(distance_from_mesh_bottom/distance_per_cell, 0) - 10, 0, resolution);

			//turn it into a voxilized mesh
			VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
			voxGen.Voxels = BitmapExtendedToCeiling(bmp, z_height);
			voxGen.Generate();
			var result = new DMesh3(MarchingCubesSmoothing(voxGen.Meshes[0], resolution));
			CentreMesh(result, offsetMold);
			ScaleMesh(result, mesh, offset);
			return result;
		}

		public static DMesh3 GenerateFinalMold(DMesh3 previewMold, DMesh3 bolus, List<AirChannel> airChannels) {
			//invert bolus normals and append it to the preview mold
			
			DMesh3 invertedBolus = new DMesh3(bolus);
			List<int> triangles = new List<int>();
			for (int i = 0; i < invertedBolus.TriangleCount; i++) triangles.Add(i);
			MeshEditor n = new MeshEditor(invertedBolus);
			n.ReverseTriangles(triangles, true);
			n.AppendMesh(previewMold);
			DMesh3 mold = n.Mesh; 

			if (airChannels.Count < 1)
				return mold;

			//convert airchannels to DMesh3
			DMesh3 channels = new DMesh3();
			MeshEditor a = new MeshEditor(channels);
			foreach (var airhole in airChannels) {
				a.AppendMesh(MeshGeometryToDMesh(airhole.Mesh));
			}
			DMesh3 tubes = new DMesh3(channels);

			//boolean subtract airchannels from mold+bolus mesh
			return new DMesh3(BolusTools.BooleanSubtraction(mold, tubes));
		}
		static DMesh3 VoxilizedMold(DMesh3 mesh, double airholeLevel, int numcells) {

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
			
		}

		static Bitmap3 BitmapExtendedToCeiling(Bitmap3 bmp, int z_height = 0) {
			int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];
			int z_top = 0;

			//getting the top layer
			for (int x = 0; x < bmp.Dimensions.x; x++) {
				for (int y = 0; y < bmp.Dimensions.y; y++) {
					for (int z = z_height; z < bmp.Dimensions.z; z++) {
						if (bmp.Get(new Vector3i(x, y, z))) {
							if (z > z_top) z_top = z;
						}
					}
				}
			}

			//if an airhole is too low, this will extend the mesh from the lowest airhole up to the top.
			//the 3D print will be easier to fill
			if (z_height > 0) {
				for (int z = z_height; z <= z_top; z++) {
					for (int x = 0; x < bmp.Dimensions.x; x++) {
						for (int y = 0; y < bmp.Dimensions.y; y++) {
							bool value = bmp.Get(new Vector3i(x, y, z - 1));
							if (value) bmp.Set(new Vector3i(x, y, z), value);
						}
					}
				}
			}

			return bmp;
		}

		static Bitmap3 BitmapBox(Bitmap3 bmp) {
		int[,,] grid = new int[bmp.Dimensions.x, bmp.Dimensions.y, bmp.Dimensions.z];

		//getting the top and bottoms
		int z_bottom = 0;
		for (int z = 0; z < bmp.Dimensions.z; z++) {
			if (z_bottom != 0) break;
				
			for (int x = 0; x < bmp.Dimensions.x; x++) {
				if (z_bottom != 0) break;

				for (int y = 0; y < bmp.Dimensions.y; y++) {
					if ( bmp.Get(new Vector3i(x, y, z))) {
						z_bottom = z;
						break;
					}
				}
			}
				
		}

		int z_top = z_bottom;

		for (int x = 0; x < bmp.Dimensions.x; x++) {
			for (int y = 0; y < bmp.Dimensions.y; y++) {
				for (int z = z_bottom; z < bmp.Dimensions.z; z++) {
					if (bmp.Get(new Vector3i(x, y, z))) {
						if (z > z_top) z_top = z;
					}
				}
			}
		}

		//if an airhole is too low, this will extend the mesh from the lowest airhole up to the top.
		//the 3D print will be easier to fill
		for (int x = 0; x < bmp.Dimensions.x; x++) {
			for (int y = 0; y < bmp.Dimensions.y; y++) {
				for (int z = z_bottom; z <= z_top; z++) {
					if (bmp.Get(new Vector3i(x, y, z))){
						for (int j = z_bottom; j <= z_top; j++) {
							bmp.Set(new Vector3i(x, y, j), true);
						}
						break;
					}
				}
			}
		}
			

		return bmp;
		}

		static void CentreMesh(DMesh3 mesh, DMesh3 originalMesh) {
			//scale the mesh to the original's size
			double scale_x = originalMesh.CachedBounds.Width / (mesh.CachedBounds.Width - 2);
			double scale_y = originalMesh.CachedBounds.Depth / (mesh.CachedBounds.Depth - 2);
			double scale_z = originalMesh.CachedBounds.Height / (mesh.CachedBounds.Height);
			MeshTransforms.Scale(mesh, scale_x, scale_y, scale_z);

			//positioning the mesh ontop of the old one
			double x = originalMesh.CachedBounds.Center.x - mesh.CachedBounds.Center.x;
			double y = originalMesh.CachedBounds.Center.y - mesh.CachedBounds.Center.y;
			double z = originalMesh.CachedBounds.Center.z - mesh.CachedBounds.Center.z;
			MeshTransforms.Translate(mesh, x, y, z);
		}

		static void ScaleMesh(DMesh3 mesh, DMesh3 originalMesh, double offset) {
			//distance to have offset mesh match
			double x = (originalMesh.CachedBounds.Max.x - originalMesh.CachedBounds.Min.x) + (offset * 2);
			double y = (originalMesh.CachedBounds.Max.y - originalMesh.CachedBounds.Min.y) + (offset * 2);
			double z = (originalMesh.CachedBounds.Max.z - originalMesh.CachedBounds.Min.z) + (offset * 2);

			//new offset mesh's distance
			double newX = (mesh.CachedBounds.Max.x - mesh.CachedBounds.Min.x);
			double newY = (mesh.CachedBounds.Max.y - mesh.CachedBounds.Min.y);
			double newZ = (mesh.CachedBounds.Max.z - mesh.CachedBounds.Min.z);

			//scaling
			Vector3d scale = new Vector3d();
			scale.x = x / newX;
			scale.y = y / newY;
			scale.z = z / newZ;

			MeshTransforms.Scale(mesh, scale, Vector3d.Zero);
		}

		static DMesh3 MarchingCubesSmoothing(DMesh3 mesh, int numcells) {
			double cell_size = mesh.CachedBounds.MaxDim / (numcells );

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

		static Bitmap3 MeshBitmap(DMesh3 mesh, int numcells) {
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
			return bmp;
		}
	}
}

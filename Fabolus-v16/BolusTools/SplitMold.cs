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
        public static DMesh3 GenerateSplitMold(DMesh3 previewMold, DMesh3 bolus, List<AirChannel> airChannels) {
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
            DMesh3 mesh = new DMesh3(BolusTools.BooleanSubtraction(mold, tubes));

            int resolution = 256;
            //bitmap use to define how the mesh voxilization goes
            Bitmap3 bmp = MeshBitmap(mesh, resolution);

            //turn it into a voxilized mesh
            VoxelSurfaceGenerator voxGen = new VoxelSurfaceGenerator();
            voxGen.Voxels = PartingToolMesh(bmp);
            voxGen.Generate();
            var result = new DMesh3(MarchingCubesSmoothing(voxGen.Meshes[0], resolution));
            CentreMesh(result, mesh);
            ScaleMesh(result, mesh, 1.0f);

            return new DMesh3(BolusTools.BooleanSubtraction(mold, result));
        }


        static Bitmap3 PartingToolMesh(Bitmap3 sourceBitmap) {
            Bitmap3 result = new Bitmap3(new Vector3i(sourceBitmap.Dimensions.x, sourceBitmap.Dimensions.y, sourceBitmap.Dimensions.z));

            for (int z = 0; z < sourceBitmap.Dimensions.z; z++) {
                for (int x = 0; x < sourceBitmap.Dimensions.x; x++) {
                    for (int y = 0; y < sourceBitmap.Dimensions.y; y++) {

                        if (sourceBitmap.Get(new Vector3i(x, y, z)) || !IsSpotAbove(sourceBitmap, x, y, z)) {
                            result.Set(new Vector3i(x, y, z), false);
                            continue;
                        }

                        result.Set(new Vector3i(x, y, z), IsSpotBelow(sourceBitmap, x, y, z));
                    }
                }
            }

            for (int z = 0; z < sourceBitmap.Dimensions.z; z++) {
                for (int x = 0; x < sourceBitmap.Dimensions.x; x++) {
                    for (int y = 0; y < sourceBitmap.Dimensions.y; y++) {

                        if (!result.Get(new Vector3i(x, y, z))) {
                            result.Set(new Vector3i(x, y, z), true);
                            if (y + 1 < sourceBitmap.Dimensions.y) {
                                result.Set(new Vector3i(x, y + 1, z), true);
                            }
                            break;
                        }
                    }
                }
            }

            return result;
		}

        static bool IsSpotAbove(Bitmap3 bmp, int xCell, int yCell, int zCell) {

            for (int y = yCell; y < bmp.Dimensions.y; y++) {
                if (bmp.Get(new Vector3i(xCell, y, zCell))) {
                    return true;
                }
            }

            return false;
		}

        static bool IsSpotBelow(Bitmap3 bmp, int xCell, int yCell, int zCell) {

            for (int y = yCell; y > 0; y--) {
                if (bmp.Get(new Vector3i(xCell, y, zCell))) {
                    return true;
                }
            }

            return false;
        }
    }
}

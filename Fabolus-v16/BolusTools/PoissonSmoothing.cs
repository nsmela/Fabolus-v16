using g3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Fabolus_v16 {
	public static partial class BolusTools {
        private static string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string ReconstructorFilePath = AppDomain.CurrentDomain.BaseDirectory + @"PoissonRecon.exe";
        public static DMesh3 PoissonSmoothing(DMesh3 mesh) {

            //check the poisson reconstructor exists
            if (!File.Exists(ReconstructorFilePath)) {
				MessageBox.Show(String.Format("Poisson Reconstructor at {0} was not found!", ReconstructorFilePath), "Varian Developer");
				return null;
			}

			//create temp folder where exe is located
			string tempFolder = BaseDirectory + @"temp\";
			Directory.CreateDirectory(tempFolder);

            //export ply file to temp folder
            SaveDMeshToPLYFile(mesh, tempFolder + @"temp.ply");

			//run poisson reconstructor
			ExecutePoisson(tempFolder + @"temp.ply", tempFolder + @"temp_smooth");

            //load new mesh from ply in folder
            var result = ReadPLYFileToDMesh(tempFolder + @"temp_smooth.ply");
            return result;
		}

        //---------------------------------------------------------------------------------------------
        /// <summary>
        /// This method saves the given MeshGeometry3D to the given file in the PLY format
        /// Calculates vertex normals required for poisson reconstruction
        /// </summary>
        /// <param name="mesh">Trianglemesh to export</param>
        /// <param name="outputFileName">Name of the file to write.</param>
        //---------------------------------------------------------------------------------------------
        private static void SaveDMeshToPLYFile(DMesh3 mesh, string outputFileName) {
            if (mesh == null)
                return;

            MeshNormals.QuickCompute(mesh);

            if (File.Exists(outputFileName)) {
                File.SetAttributes(outputFileName, FileAttributes.Normal);
                File.Delete(outputFileName);
            }

            using (TextWriter writer = new StreamWriter(outputFileName)) {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("element vertex " + mesh.VertexCount);

                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property float nx");
                writer.WriteLine("property float ny");
                writer.WriteLine("property float nz");

                writer.WriteLine("element face " + mesh.TriangleCount);

                writer.WriteLine("property list uchar int vertex_indices");

                writer.WriteLine("end_header");

                for (int v = 0; v < mesh.VertexCount; v++) {
                    Vector3d normal = mesh.GetVertexNormal(v);

					writer.Write(mesh.GetVertex(v).x.ToString("e") + " ");
                    writer.Write(mesh.GetVertex(v).y.ToString("e") + " ");
                    writer.Write(mesh.GetVertex(v).z.ToString("e") + " ");
                    writer.Write(normal.x.ToString("e") + " ");
                    writer.Write(normal.y.ToString("e") + " ");
                    writer.Write(normal.z.ToString("e"));

                    writer.WriteLine();
                }

                int i = 0;
                while (i < mesh.TriangleCount) {
                    var triangle = mesh.GetTriangle(i);

                    writer.Write("3 ");
                    writer.Write(triangle.a + " ");
                    writer.Write(triangle.b + " ");
                    writer.Write(triangle.c + " ");
                    writer.WriteLine();
                    i++;
                }
            }
        }

        private static void ExecutePoisson(string inputFile, string outputFile, int depth = 6, float scale = 1.2f, int samples = 1) {
            if (File.Exists(inputFile)) {
                //Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = ReconstructorFilePath;
                startInfo.Arguments = String.Format(@" --in ""{0}"" --out ""{1}"" --depth {2} --scale {3} --samplesPerNode {4}",
                    inputFile, //in
                    outputFile, //out
                    depth.ToString(), //depth
                    scale.ToString(), //scale
                    samples.ToString()); //samples

                //send the command
                try {
                    using (Process exeProcess = Process.Start(startInfo)) {
                        exeProcess.WaitForExit();
                    }

                } catch (Exception ex) {
                    MessageBox.Show("Recon failed! " + ex.Message);
                }
            }
        }

        private static DMesh3 ReadPLYFileToDMesh(string filepath) {
            //verify file exists
            if (File.Exists(filepath)) {
                List<string> headers = new List<string>();

                bool endheader = false;
                using (BinaryReader b = new BinaryReader(File.Open(filepath, FileMode.Open))) {
                    //reads the header
                    while (!endheader) {
                        string line = ReadReturnTerminatedString(b);
                        headers.Add(line);
                        if (line == "end_header") {
                            endheader = true;
                        }
                    }

                    //determining the vertexes and faces
                    int vertexRef = headers.FindIndex(element => element.StartsWith("element vertex", StringComparison.Ordinal));
                    string text = headers[vertexRef].Substring(headers[vertexRef].LastIndexOf(' ') + 1);
                    int number_of_vertexes = Convert.ToInt32(text);

                    int faceRef = headers.FindIndex(element => element.StartsWith("element face", StringComparison.Ordinal));
                    text = headers[faceRef].Substring(headers[faceRef].LastIndexOf(' ') + 1);
                    int number_of_faces = Convert.ToInt32(text);

                    //read the vertexes
                    DMesh3 mesh = new DMesh3(true); //want normals
                    for (int i = 0; i < number_of_vertexes; i++) {
                        float x, y, z;
                        x = b.ReadSingle();
                        y = b.ReadSingle();
                        z = b.ReadSingle();

                        mesh.AppendVertex(new Vector3d(x, y, z));
                    }

                    //read the faces
                    for (int i = 0; i < number_of_faces; i++) {
                        b.ReadByte();//skips the first bye, always '3'
                        int v0 = b.ReadInt32();
                        int v1 = b.ReadInt32();
                        int v2 = b.ReadInt32();

                        mesh.AppendTriangle(v0, v1, v2);
                    }

                    return mesh;
                }

            } else {
                MessageBox.Show("The file " + filepath + " does not exist!");
            }
            return null;
        }

        private static string ReadReturnTerminatedString(BinaryReader stream) {
            string str = "";
            char ch;
            while ((int)(ch = stream.ReadChar()) != 10)
                str = str + ch;
            return str;
        }


        
    }
}

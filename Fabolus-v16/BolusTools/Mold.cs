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
        public static MeshGeometry3D GenerateBolusMold(MeshGeometry3D mesh, double voxelSize, Matrix3D transform) {

            Point3DCollection positions = new();
            foreach (Point3D p in mesh.Positions)
                positions.Add(transform.Transform(p));
            
            //used for boundries for the mesh after transformation
            double zMax = 0;
            double zMin = 0;
            foreach (Point3D p in positions) {
                if (p.Z > zMax)
                    zMax = p.Z;
                if (p.Z < zMin)
                    zMin = p.Z;
            }

            //make the points 2d
            var points = GetContour(positions, voxelSize);

            //create mesh
            return GetMold(points, zMax, zMin);

        }

        /// <summary>
        /// Gets 2D shape of the mesh. Checks all verticies regardless of z axis. Uses a grid-based approach: 
        /// breaks an area up into x-y grid and if it detects one or more points in that square, it is considered filled
        /// The countour is around these filled squares
        /// </summary>
        /// <param name="points">The points from a mesh</param>
        /// <param name="size">The resolution size</param>
        /// <returns>List of 2D points for the contour</returns>
        private static List<Point> GetContour(Point3DCollection points, double size) {
            if (points.Count > 0) {
                double offset = 3; //used to give some space at the start of the contour boxes
                double bottom_x, top_x, bottom_y, top_y;

                bottom_x = points.Min(p => p.X);
                top_x = points.Max(p => p.X) - bottom_x;
                bottom_y = points.Min(p => p.Y);
                top_y = points.Max(p => p.Y) - bottom_y;

                double gridsize = size;
                double start_x = bottom_x - gridsize - offset;
                double start_y = bottom_y - gridsize - offset;

                int x_grid = Convert.ToInt16((top_x - start_x) / gridsize) + 1;
                int y_grid = Convert.ToInt16((top_y - start_y) / gridsize) + 1;
                bool[,] contourgrid = new bool[x_grid, y_grid];

                //finds which box areas contain the model's positions
                for (int y = 0; y < y_grid; y++) {
                    double lowY = start_y + y * gridsize;
                    double highY = lowY + gridsize;
                    Point3DCollection points_y = new();
                    foreach (Point3D p in points)
                        if (p.Y > lowY && p.Y < highY)
                            points_y.Add(p);

                    for (int x = 0; x < x_grid; x++) {
                        double lowX = start_x + x * gridsize;
                        double highX = lowX + gridsize;
                        foreach (Point3D p in points_y)
                            if (p.X > lowX && p.X < highX) {
                                contourgrid[x, y] = true;
                                break;
                            }
                    }

                }

                FillHoles(contourgrid);

                //create contour points by finding boxes that are adjacent to the edge of the good boxes
                List<Point> contourPointsList = new List<Point>();
                for (int y = 0; y < y_grid; y++)
                    for (int x = 0; x < x_grid; x++) {
                        int sides = AdjacentGoodBoxes(x, y, contourgrid);
                        if (sides > 0 && sides < 4 && !contourgrid[x, y])//beside a good box and not a good box itself
                        {
                            Point point = new Point(start_x + x * gridsize + gridsize / 2, start_y + y * gridsize + gridsize / 2);
                            contourPointsList.Add(point);
                        }
                    }

                return contourPointsList;

            }

            return null;
        }

        //calculates a mold box to contour around the input mesh
        private static MeshGeometry3D GetMold(List<Point> points, double zMax, double zMin) {
            if (points != null && points.Count > 0) {
                double z_height = zMax + 4; //highest point for the mold
                double offsetZ = zMin - 3;

                //the final contour
                MeshBuilder mb = new MeshBuilder() {
                    CreateNormals = false,
                    CreateTextureCoordinates = false
                };

                points = SortPoints(points);

                //create contour face
                var result = CuttingEarsTriangulator.Triangulate(points);

                if (result == null)
                    return null;

                //create lower surface
                for (int t = 2; t < result.Count; t += 3) {
                    Point3D p0 = new Point3D(points[result[t]].X, points[result[t]].Y, offsetZ);
                    Point3D p1 = new Point3D(points[result[t - 1]].X, points[result[t - 1]].Y, offsetZ);
                    Point3D p2 = new Point3D(points[result[t - 2]].X, points[result[t - 2]].Y, offsetZ);

                    mb.AddTriangle(p0, p1, p2);
                }

                //create higher surface
                //point order is reversed to reverse the normals created
                double upper_offsetZ = z_height;
                for (int t = 2; t < result.Count; t += 3) {
                    Point3D p0 = new Point3D(points[result[t - 2]].X, points[result[t - 2]].Y, upper_offsetZ);
                    Point3D p1 = new Point3D(points[result[t - 1]].X, points[result[t - 1]].Y, upper_offsetZ);
                    Point3D p2 = new Point3D(points[result[t]].X, points[result[t]].Y, upper_offsetZ);

                    mb.AddTriangle(p0, p1, p2);
                }

                points.Add(points[0]);

                //create walls
                for (int t = 1; t < points.Count; t++) {
                    Point3D p0 = new Point3D(points[t - 1].X, points[t - 1].Y, offsetZ);
                    Point3D p1 = new Point3D(points[t].X, points[t].Y, offsetZ);
                    Point3D p2 = new Point3D(points[t - 1].X, points[t - 1].Y, upper_offsetZ);

                    mb.AddTriangle(p0, p1, p2);

                    p0 = new Point3D(points[t].X, points[t].Y, upper_offsetZ);
                    p1 = new Point3D(points[t - 1].X, points[t - 1].Y, upper_offsetZ);
                    p2 = new Point3D(points[t].X, points[t].Y, offsetZ); ;

                    mb.AddTriangle(p0, p1, p2);
                }

                return mb.ToMesh();
            }

            return null;
        }

        /// <summary>
        /// Finds any empty squares within the contour's grid map and labels them as filled
        /// </summary>
        /// <param name="contourgrid"></param>
        private static void FillHoles(bool[,] contourgrid) {
            bool filled = false;
            int xMax = contourgrid.GetLength(0);
            int yMax = contourgrid.GetLength(1);

            //fill any empty spaces with 3 or more good adjacent boxes
            for (int y = 0; y < yMax; y++)
                for (int x = 0; x < xMax; x++)
                    if (!contourgrid[x, y]) {
                        int sides = AdjacentGoodBoxes(x, y, contourgrid);
                        if (sides > 2) {
                            filled = true;
                            contourgrid[x, y] = true;
                        }
                    }

            //if one or more empty spaces were filled, check again
            if (filled)
                FillHoles(contourgrid);
        }

        //determines how many filled boxes are surrounding the box
        private static int AdjacentGoodBoxes(int x, int y, bool[,] contourgrid) {
            int sides = 0;

            //left 
            if (x != 0)
                if (contourgrid[x - 1, y])
                    sides++;

            //top
            if (y < contourgrid.GetLength(1) - 1)
                if (contourgrid[x, y + 1])
                    sides++;

            //right
            if (x < contourgrid.GetLength(0) - 1)
                if (contourgrid[x + 1, y])
                    sides++;

            //bottom
            if (y != 0)
                if (contourgrid[x, y - 1])
                    sides++;

            return sides;
        }
        //finds the closest point around a contour
        private static List<Point> SortPoints(List<Point> contourpoints) {
            //creating a list that will eventually have entries removed to speed things up
            List<Point> points = new();
            foreach (Point p in contourpoints)
                points.Add(new Point(p.X, p.Y));

            List<Point> result = new();

            Point start = new Point(points[0].X, points[0].Y);
            result.Add(start);
            points.RemoveAt(0);

            //cycle through and reorganize points into result
            List<double> distances = new List<double>();
            List<int> indexes = new List<int>();

            while (points.Count > 0) {
                distances.Clear();
                indexes.Clear();

                foreach (Point p in points) {
                    double distance = GetDistance(result[result.Count - 1], p);
                    distances.Add(distance);
                    indexes.Add(points.FindIndex(x => x == p));
                }

                int index = indexes[distances.FindIndex(d => d == distances.Min())];
                result.Add(points[index]);
                points.RemoveAt(index);
            }

            return result;
        }

        //distance from one 2D point to another
        private static double GetDistance(Point p1, Point p2) {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }

    }
}

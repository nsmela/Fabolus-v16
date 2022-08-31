using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using g3;

namespace Fabolus_v16.MVVM.Models {
	public class AirChannel {
		private Point3D _anchor;
		private double _radius;
		private float _length;

		public Point3D Anchor { get => _anchor; set => _anchor = value; }
		public double Radius { get => _radius; set => _radius = value; }
		public float Length { get => _length; set => _length = value; }
		private Vector3d point { get => new Vector3d(Anchor.X, Anchor.Y, Anchor.Z - 2); }


		public AirChannel(Point3D anchor, double radius, float length) {
				Anchor = anchor;
				Radius = radius;
				Length = length;
		}

		//used to create an enlarged meshes for boolean union
		public MeshGeometry3D OffsetMesh(float offset) {
			MeshBuilder mesh = new MeshBuilder(true);
			mesh.AddSphere(Anchor, Radius);
			mesh.AddCylinder(Anchor, new Point3D(Anchor.X, Anchor.Y, Anchor.Z + Length - 1), Radius + offset, 32, false, true);
			return mesh.ToMesh();
		}

		public MeshGeometry3D Mesh {
			get {
				MeshBuilder mesh = new MeshBuilder(true);
				mesh.AddSphere(Anchor, Radius);
				mesh.AddCylinder(Anchor, new Point3D(Anchor.X, Anchor.Y, Anchor.Z + Length), Radius, 32, false, true);
				return mesh.ToMesh();
			}
		}

		public DMesh3 DMesh {
			get {
				DMesh3 result = new DMesh3();
				MeshEditor m = new MeshEditor(result);

				Sphere3Generator_NormalizedCube generateSphere = new Sphere3Generator_NormalizedCube() { Radius = this.Radius, EdgeVertices = 5 };
				DMesh3 sphereMesh = generateSphere.Generate().MakeDMesh();
				MeshTransforms.Translate(sphereMesh, point);
				m.AppendMesh(sphereMesh);

				CappedCylinderGenerator generateCylinder = new CappedCylinderGenerator() {
					BaseRadius = (float)this.Radius,
					TopRadius = (float)this.Radius,
					Height = this.Length,
					Slices = 64
				};
				DMesh3 tube = generateCylinder.Generate().MakeDMesh();
				Quaterniond rotate = new Quaterniond(Vector3d.AxisX, 90.0f);
				MeshTransforms.Rotate(tube, Vector3d.Zero, rotate);
				MeshTransforms.Translate(tube, point);
				m.AppendMesh(tube);

				return m.Mesh;
			}
		}

	}
}

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


		public AirChannel(Point3D anchor, double radius, float length) {
				Anchor = anchor;
				Radius = radius;
				Length = length;
		}

		public MeshGeometry3D Mesh {
			get {
				MeshBuilder mesh = new MeshBuilder(true);
				mesh.AddSphere(Anchor, Radius);
				mesh.AddCylinder(Anchor, new Point3D(Anchor.X, Anchor.Y, Anchor.Z + Length), Radius, 32, false, true);
				return mesh.ToMesh();
			}
		}

	}
}

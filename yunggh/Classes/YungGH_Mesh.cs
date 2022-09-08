using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    internal partial class YungGH
    {
        /// <summary>
        /// A simple mesh cube
        /// </summary>
        /// <param name="A">bottom front right point</param>
        /// <param name="B">bottom back right point</param>
        /// <param name="C">bottom back left point</param>
        /// <param name="D">bottom front left point</param>
        /// <param name="E">top front right point</param>
        /// <param name="F">top back right point</param>
        /// <param name="G">top back left point</param>
        /// <param name="H">top front left point</param>
        /// <returns>A simple quad mesh "cube" with the input points as vertices</returns>
        public Mesh MeshCube(Point3d A, Point3d B, Point3d C, Point3d D, Point3d E, Point3d F, Point3d G, Point3d H)
        {
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            mesh.Vertices.Add(A); //0
            mesh.Vertices.Add(B); //1
            mesh.Vertices.Add(C); //2
            mesh.Vertices.Add(D); //3
            mesh.Vertices.Add(E); //4
            mesh.Vertices.Add(F); //5
            mesh.Vertices.Add(G); //6
            mesh.Vertices.Add(H); //7

            mesh.Faces.AddFace(7, 6, 5, 4);
            mesh.Faces.AddFace(1, 0, 4, 5);
            mesh.Faces.AddFace(2, 1, 5, 6);
            mesh.Faces.AddFace(3, 2, 6, 7);
            mesh.Faces.AddFace(0, 3, 7, 4);

            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }
    }
}
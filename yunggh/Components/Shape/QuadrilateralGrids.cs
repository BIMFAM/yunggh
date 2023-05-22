using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Geometry;

namespace yunggh
{
    public class QuadrilateralGrids : GH_Component
    {
        public QuadrilateralGrids()
          : base("Quadrilateral Grids", "QuadrilateralGrids",
              "Create Quadrilateral Grids",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Input points", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Medial axis curves", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> points = new List<Point3d>();
            if (!DA.GetDataList(0, points)) return;
            if (points.Count == 0) return;
            Mesh m = getDelaunay(points);
            List<LineCurve> lst = new List<LineCurve>();
            m.Faces.ConvertTrianglesToQuads(0, 0);
            for (int i = 0; i < m.Faces.Count; i++)
            {
                Point3f pt1, pt2, pt3, pt4;
                m.Faces.GetFaceVertices(i, out pt1, out pt2, out pt3, out pt4);
                Point3d ct = m.Faces.GetFaceCenter(i);

                if (m.Faces[i].IsTriangle)
                {

                    LineCurve lc1 = new LineCurve(pt1, pt2);
                    LineCurve lc2 = new LineCurve(pt2, pt3);
                    LineCurve lc3 = new LineCurve(pt3, pt1);

                    lst.Add(lc1);
                    lst.Add(lc2);
                    lst.Add(lc3);
                    lst.Add(new LineCurve(lc1.PointAtNormalizedLength(0.5), ct));
                    lst.Add(new LineCurve(lc2.PointAtNormalizedLength(0.5), ct));
                    lst.Add(new LineCurve(lc3.PointAtNormalizedLength(0.5), ct));

                }
                else
                {
                    LineCurve lc1 = new LineCurve(pt1, pt2);
                    LineCurve lc2 = new LineCurve(pt2, pt3);
                    LineCurve lc3 = new LineCurve(pt3, pt4);
                    LineCurve lc4 = new LineCurve(pt4, pt1);

                    lst.Add(lc1);
                    lst.Add(lc2);
                    lst.Add(lc3);
                    lst.Add(lc4);

                    lst.Add(new LineCurve(lc1.PointAtNormalizedLength(0.5), ct));
                    lst.Add(new LineCurve(lc2.PointAtNormalizedLength(0.5), ct));
                    lst.Add(new LineCurve(lc3.PointAtNormalizedLength(0.5), ct));
                    lst.Add(new LineCurve(lc4.PointAtNormalizedLength(0.5), ct));

                }
            }
            DA.SetDataList(0, lst);
        }

        Mesh getDelaunay(List<Point3d> pts)
        {
            //convert point3d to node2
            //grasshopper requres that nodes are saved within a Node2List for Delaunay
            var nodes = new Grasshopper.Kernel.Geometry.Node2List();
            for (int i = 0; i < pts.Count; i++)
            {
                //notice how we only read in the X and Y coordinates
                //  this is why points should be mapped onto the XY plane
                nodes.Append(new Grasshopper.Kernel.Geometry.Node2(pts[i].X, pts[i].Y));
            }

            //solve Delaunay
            var delMesh = new Mesh();
            var faces = new List<Grasshopper.Kernel.Geometry.Delaunay.Face>();

            faces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(nodes, 1);

            //output
            delMesh = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Mesh(nodes, 1, ref faces);
            return delMesh;
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.MedialAxis;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("298F63D7-7DF1-418D-8FCB-8561626230EF"); }
        }
    }
}
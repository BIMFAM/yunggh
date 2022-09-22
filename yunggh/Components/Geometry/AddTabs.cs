using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    public class AddTabs : GH_Component
    {
        public AddTabs()
          : base("Add Tabs", "AddTabs",
              "Add Tabs to polylines and sort geometry for fabrication.",
              "yung gh", "Geometry")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Unroll", "U", "Unrolled Polysurfaces.", GH_ParamAccess.tree);
            pManager.AddPointParameter("Points", "P", "Unrolled Points.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Width", "W", "Tab Width", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Type", "A", "Tab Type", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "T", "Tab Parameter", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Unroll", "U", "Unrolled Polysurfaces", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Naked Edge", "N", "Naked Edges", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Interior Edge", "I", "Interior Edges", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            GH_Structure<GH_Brep> unrollData = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Point> pointsData = new GH_Structure<GH_Point>();
            double W = 0;
            double T = 0;
            int A = 0;
            if (!DA.GetDataTree(0, out unrollData)) return;
            if (!DA.GetDataTree(1, out pointsData)) return;
            if (!DA.GetData(2, ref W)) return;
            if (!DA.GetData(3, ref A)) return;
            if (!DA.GetData(4, ref T)) return;

            //setup output data structures
            GH_Structure<GH_Brep> tabbedUnrolls = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Point> unrollPts = new GH_Structure<GH_Point>();
            GH_Structure<GH_Curve> nakedEdges = new GH_Structure<GH_Curve>();
            GH_Structure<GH_Curve> interiorEdges = new GH_Structure<GH_Curve>();

            //main function

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double width = W;
            double parameter = Math.Max(0, T);
            parameter = Math.Min(0.499, T);

            for (int p = 0; p < unrollData.Count(); p++)
            {
                GH_Path path = unrollData.get_Path(p);
                List<GH_Brep> breps = unrollData.get_Branch(p).Cast<GH_Brep>().ToList();
                List<GH_Point> tabPts = pointsData.get_Branch(p).Cast<GH_Point>().ToList();

                foreach (GH_Brep brepGH in breps)
                {
                    Brep brep = null;
                    if (!GH_Convert.ToBrep_Primary(brepGH, ref brep)) { continue; }

                    //get edges
                    List<Curve> edges = new List<Curve>();
                    Curve[] loops = brep.DuplicateNakedEdgeCurves(true, true);
                    //get points for unroll
                    foreach (Curve curve in loops)
                    {
                        Curve[] segments = curve.DuplicateSegments();
                        edges.AddRange(segments);
                    }

                    //get interior edges
                    var allEdges = brep.Edges;
                    foreach (BrepEdge brepEdge in allEdges)
                    {
                        if (brepEdge.Valence != EdgeAdjacency.Interior) { continue; }

                        GH_Curve curveGH = new GH_Curve();
                        if (!GH_Convert.ToGHCurve_Primary(brepEdge.ToNurbsCurve(), ref curveGH)) { continue; }
                        interiorEdges.Append(curveGH, path);
                    }

                    //add naked edges without tabPoints
                    foreach (Curve edge in edges)
                    {
                        bool cpFound = false;
                        foreach (GH_Point tabPtGH in tabPts)
                        {
                            Point3d tabPt = Point3d.Unset;
                            if (!GH_Convert.ToPoint3d_Primary(tabPtGH, ref tabPt)) { continue; }

                            double t;
                            edge.ClosestPoint(tabPt, out t);
                            Point3d cp = edge.PointAt(t);
                            double dist = tabPt.DistanceTo(cp);
                            if (dist > 0.01) { continue; }
                            cpFound = true;
                            break;
                        }
                        if (cpFound) { continue; }
                        GH_Curve curveGH = null;
                        if (!GH_Convert.ToGHCurve_Primary(edge, ref curveGH)) { continue; }
                        nakedEdges.Append(curveGH, path);
                    }

                    //using points as drivers
                    //get a tab based on the edge
                    List<Brep> tabs = new List<Brep>();
                    foreach (GH_Point tabPtGH in tabPts)
                    {
                        Point3d tabPt = Point3d.Unset;
                        if (!GH_Convert.ToPoint3d_Primary(tabPtGH, ref tabPt)) { continue; }

                        //get closest edge
                        Curve edge = GetClosestEdge(tabPt, edges);

                        //get normal from brep
                        Vector3d normal = GetEdgeNormal(tabPt, edge, brep);
                        if (normal == Vector3d.Zero) { continue; }

                        //get eight control points
                        Point3d[] controls = GetControlPoints(edge, normal, width, parameter);
                        foreach (Point3d pt in controls)
                        {
                            GH_Point ptGH = null;
                            if (!GH_Convert.ToGHPoint_Primary(pt, ref ptGH)) { continue; }
                            unrollPts.Append(ptGH, path);
                        }

                        //create tab profile (based on type)
                        Curve naked;
                        Curve interior;
                        Polyline tabCrv = CreateTab(controls, A, out naked, out interior);
                        Brep[] bf = Brep.CreatePlanarBreps(tabCrv.ToNurbsCurve(), tolerance);
                        if (bf != null) { if (bf.Length > 0) { tabs.Add(bf[0]); } }

                        //add edges
                        if (naked != null)
                        {
                            GH_Curve curveGH = null;
                            if (GH_Convert.ToGHCurve_Primary(naked, ref curveGH)) { nakedEdges.Append(curveGH, path); }
                        }
                        if (interior != null)
                        {
                            GH_Curve curveGH = null;
                            if (GH_Convert.ToGHCurve_Primary(interior, ref curveGH)) { interiorEdges.Append(curveGH, path); }
                        }
                    }

                    //join tabs to brep
                    tabs.Add(brep);
                    Brep[] joins = Brep.JoinBreps(tabs, tolerance);
                    var joinsGH = MultiUnroll.ConvertToGH(joins);
                    tabbedUnrolls.AppendRange(joinsGH, path);
                }
            }

            //output
            DA.SetDataTree(0, tabbedUnrolls); //U
            DA.SetDataTree(1, nakedEdges); //N
            DA.SetDataTree(2, interiorEdges); //I
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.AddTabs;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("2EBF01D7-34E5-4FAC-B9A8-FD3B496EEA5E"); }
        }

        //methods
        private Polyline CreateTab(Point3d[] pts, int type, out Curve naked, out Curve interior)
        {
            //initial assignments
            Polyline polyline = new Polyline();
            Polyline nakedPolyline = new Polyline();
            Line interiorLine;
            naked = null;
            interior = null;

            switch (type)
            {
                case 1://square = 1
                    polyline.AddRange(new List<Point3d>() { pts[6], pts[7], pts[9], pts[8], pts[6] });
                    interiorLine = new Line(pts[6], pts[7]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[6], pts[8], pts[9], pts[7], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                case 2://rectangle = 2
                    polyline.AddRange(new List<Point3d>() { pts[0], pts[1], pts[3], pts[2], pts[0] });
                    interiorLine = new Line(pts[0], pts[1]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[2], pts[3], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                case 3://taper = 3
                    polyline.AddRange(new List<Point3d>() { pts[0], pts[1], pts[9], pts[8], pts[0] });
                    interiorLine = new Line(pts[0], pts[1]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[8], pts[9], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                case 4://triangle = 4
                    polyline.AddRange(new List<Point3d>() { pts[6], pts[7], pts[5], pts[6] });
                    interiorLine = new Line(pts[6], pts[7]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[6], pts[5], pts[7], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                case 5://edge triangle = 5
                    polyline.AddRange(new List<Point3d>() { pts[0], pts[1], pts[3], pts[0] });
                    interiorLine = new Line(pts[0], pts[1]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[3], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                case 6://edge triangle flipped = 6
                    polyline.AddRange(new List<Point3d>() { pts[0], pts[1], pts[2], pts[0] });
                    interiorLine = new Line(pts[0], pts[1]);
                    interior = interiorLine.ToNurbsCurve();
                    nakedPolyline.AddRange(new List<Point3d>() { pts[0], pts[2], pts[1] });
                    naked = nakedPolyline.ToNurbsCurve();
                    break;

                default:
                    return null;
            }
            return polyline;
        }

        private Point3d[] GetControlPoints(Curve edge, Vector3d normal, double offset, double t)
        {
            Vector3d vec = normal * offset;

            //get points at start and end
            Point3d[] controls = new Point3d[10];

            //ends
            controls[0] = edge.PointAtEnd;
            controls[1] = edge.PointAtStart;
            controls[2] = controls[0] + vec;
            controls[3] = controls[1] + vec;

            //mid
            controls[4] = (controls[0] + controls[1]) / 2.00;
            controls[5] = (controls[2] + controls[3]) / 2.00;

            //create line from edge FIX
            Line line = new Line(controls[0], controls[1]);
            edge = line.ToNurbsCurve();

            //parameters
            controls[6] = edge.PointAt(t * edge.GetLength());
            controls[7] = edge.PointAt((1 - t) * edge.GetLength());
            controls[8] = controls[6] + vec;
            controls[9] = controls[7] + vec;

            return controls;
        }

        private Vector3d GetEdgeNormal(Point3d pt, Curve edge, Brep brep)
        {
            //get offset edges
            Curve[] offset1s = edge.Offset(Plane.WorldXY, 0.01, 0.001, CurveOffsetCornerStyle.None);
            Curve[] offset2s = edge.Offset(Plane.WorldXY, -0.01, 0.001, CurveOffsetCornerStyle.None);

            if (offset1s == null || offset2s == null) { return Vector3d.Zero; }
            if (offset1s.Length == 0 || offset2s.Length == 0) { return Vector3d.Zero; }

            Curve offset1 = offset1s[0];
            Curve offset2 = offset2s[0];

            //get closest point for each offset
            double t;
            offset1.ClosestPoint(pt, out t);
            Point3d pt1 = offset1.PointAt(t);
            offset2.ClosestPoint(pt, out t);
            Point3d pt2 = offset2.PointAt(t);

            //find which point is farthest from brep
            Point3d brep1 = brep.ClosestPoint(pt1);
            Point3d brep2 = brep.ClosestPoint(pt2);

            Vector3d normal = pt1 - pt;
            if (brep1.DistanceTo(pt1) < brep2.DistanceTo(pt2))
            {
                normal = pt2 - pt;
            }

            normal.Unitize();
            return normal;
        }

        private Curve GetClosestEdge(Point3d pt, List<Curve> edges)
        {
            //guard statement
            if (edges.Count == 0) { return null; }

            double t;
            Curve closestEdge = edges[0];
            closestEdge.ClosestPoint(pt, out t);
            Point3d cp = closestEdge.PointAt(t);
            double dist = pt.DistanceTo(cp);

            for (int i = 1; i < edges.Count; i++)
            {
                Curve testEdge = edges[i];
                testEdge.ClosestPoint(pt, out t);
                cp = testEdge.PointAt(t);
                if (cp.DistanceTo(pt) > dist) { continue; }

                closestEdge = testEdge;
                dist = cp.DistanceTo(pt);
            }

            return closestEdge;
        }
    }
}
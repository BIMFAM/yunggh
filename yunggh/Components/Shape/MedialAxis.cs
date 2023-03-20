using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino;
using Rhino.Geometry.Intersect;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Geometry;

namespace yunggh
{
    public class MedialAxis : GH_Component
    {
        public Vector3d up = new Vector3d(0, 0, 1);
        public Vector3d down = new Vector3d(0, 0, -1);
        public MedialAxis()
          : base("Medial Axis", "MedialAxis",
              "Calculate Medial Axis",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve for erosion", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Mdeial Axis", "M", "Medial axis curves", GH_ParamAccess.list);
            //pManager.AddCurveParameter("Curve", "C", "Curve of Voronoi", GH_ParamAccess.list);
            //pManager.AddMeshParameter("Mesh", "M", "Mesh of curve", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            const double TOL = 1;
            Curve crv = null;
            if (!DA.GetData(0, ref crv)) return;

            //List<Circle> circleList = new List<Circle>();
            List<Point3d> pointsList = getValidPoints(crv, TOL, TOL);
            //BoundingBox bbox = crv.GetBoundingBox(true);

            Mesh mesh = Mesh.CreateFromPlanarBoundary(crv, new MeshingParameters(), TOL);

            List<LineCurve> lcvlist = new List<LineCurve>();
            foreach (Polyline pl in getVoronoi(pointsList))
            {
                foreach (Line l in pl.GetSegments())
                {
                    lcvlist.Add(new LineCurve(l));
                }
            }


            List<LineCurve> medialLines = new List<LineCurve>();

            for (int i = 0; i < lcvlist.Count; i++) {
                if (!PointOnMesh(mesh, lcvlist[i].PointAt(0))) continue;
                if (!PointOnMesh(mesh, lcvlist[i].PointAt(1))) continue;
                CurveIntersections intersections = Intersection.CurveCurve(
                        crv,
                        lcvlist[i],
                        RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                        Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance
                );

                if (intersections.Count != 0) continue;

                medialLines.Add(lcvlist[i]);
            }


            DA.SetDataList(0, medialLines);  // output curves list
            //DA.SetDataList(1, lcvlist); // voronoi curves
            //DA.SetData(2, mesh);  // output mesh

        }

        List<Point3d> getControlPoints(Curve curve)
        {
            List<Point3d> list = new List<Point3d>();
            NurbsCurve nbcrv = curve.ToNurbsCurve();
            NurbsCurvePointList nbcrvlst = nbcrv.Points;
            foreach (ControlPoint cpt in nbcrvlst)
            {
                list.Add(new Point3d(cpt.X, cpt.Y, cpt.Z));
            }
            return list;
        }

        List<Point3d> getValidPoints(Curve curve, double divLen, double delLen)
        {
            List<Point3d> list = new List<Point3d>();
            List<Point3d> cpList = getControlPoints(curve);
            foreach (double len in curve.DivideByLength(divLen, false))
            {
                Point3d pt = curve.PointAt(len);
                bool add = true;
                foreach (Point3d cpt in cpList)
                {
                    if (cpt.DistanceTo(pt) < delLen)
                    {
                        add = false;
                        break;
                    }
                }
                if (add) list.Add(pt);
            }
            foreach (Point3d cpt in cpList)
            {
                list.Add(cpt);
            }
            return list;
        }

        List<Polyline> getVoronoi(List<Point3d> nodePts)
        {

            //# Create a boundingbox and get its corners
            BoundingBox bb = new BoundingBox(nodePts);
            Vector3d d = bb.Diagonal;
            double dl = d.Length;
            double f = dl / 15;
            bb.Inflate(f, f, f);
            Point3d[] bbCorners = bb.GetCorners();

            //# Create a list of nodes
            Node2List nodes = new Node2List();
            foreach (Point3d p in nodePts)
            {
                Node2 n = new Node2(p.X, p.Y);
                nodes.Append(n);
            }

            //Create a list of outline nodes using the BB
            Node2List outline = new Node2List();
            foreach (Point3d p in bbCorners)
            {
                Node2 n = new Node2(p.X, p.Y);
                outline.Append(n);
            }


            //# Calculate the delaunay triangulation
            var delaunay = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Connectivity(nodes, 0.0001, false);

            // # Calculate the voronoi diagram
            var voronoi = Grasshopper.Kernel.Geometry.Voronoi.Solver.Solve_Connectivity(nodes, delaunay, outline);

            //# Get polylines from the voronoi cells and return them to GH
            List<Polyline> polys = new List<Polyline>();
            foreach (var c in voronoi)
            {

                Polyline pl = c.ToPolyline();

                polys.Add(pl);
            }
            return polys;
        }

        public bool PointOnMesh(Mesh mesh, Point3d pt)
        {
            if (mesh == null) return false;
            //check if intersections with up vector
            Line lineUp = new Line(pt, up);
            Int32[] intersectionsUp; //where intersection locations are held
            Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, lineUp, out intersectionsUp);

            //check if intersections with down vector
            Line lineDown = new Line(pt, down);
            Int32[] intersectionsDown; //where intersection locations are held
            Rhino.Geometry.Intersect.Intersection.MeshLine(mesh, lineDown, out intersectionsDown);

            if (intersectionsUp == null && intersectionsDown == null) { return false; }
            return true;
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
            get { return new Guid("0871EAD0-544C-4E0E-98A1-DCB5C559DF11"); }
        }
    }
}
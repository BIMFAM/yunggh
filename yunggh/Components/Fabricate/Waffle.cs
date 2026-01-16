using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components.Fabricate
{
    public class Waffle : GH_Component
    {
        public Waffle()
            : base("Waffle Structure", "Waffle",
              "Create a plane oriented waffle slab structure from a closed Brep",
              "yung gh", "Fabricate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Closed Brep to create waffle slab.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Base Plane.", GH_ParamAccess.item);
            pManager.AddPointParameter("X Origins", "X", "X plane origins.", GH_ParamAccess.list);
            pManager.AddPointParameter("Y Origins", "Y", "Y plane origins.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material Thickness", "T", "Material thickness for notch width.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("X Slabs", "X", "X Slabs.", GH_ParamAccess.list);
            pManager.AddBrepParameter("Y Slabs", "Y", "Y Slabs.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            var brep = new Brep();
            var plane = Plane.Unset;
            var xOrigins = new List<Point3d>();
            var yOrigins = new List<Point3d>();
            double thickness = 0;
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetData(1, ref plane)) return;
            if (!DA.GetDataList(2, xOrigins)) return;
            if (!DA.GetDataList(3, yOrigins)) return;
            if (!DA.GetData(4, ref thickness)) return;

            //main function
            //get planes
            var xPlane = new Plane(plane.Origin, plane.YAxis, plane.ZAxis);
            var yPlane = new Plane(plane.Origin, plane.XAxis, plane.ZAxis);
            var xPlanes = GeneratePlanes(xPlane, xOrigins);
            var yPlanes = GeneratePlanes(yPlane, yOrigins);

            //cut slabs
            var xSlabs = CutSlabs(brep, xPlanes, yPlanes, thickness, plane.ZAxis);
            var ySlabs = CutSlabs(brep, yPlanes, xPlanes, thickness, -plane.ZAxis);

            //output
            DA.SetDataList(0, xSlabs);
            DA.SetDataList(1, ySlabs);
        }

        private static List<Plane> GeneratePlanes(Plane plane, List<Point3d> origins)
        {
            var planes = new List<Plane>();
            foreach (var origin in origins)
            {
                var gPlane = new Plane(plane);
                gPlane.Origin = origin;
                planes.Add(gPlane);
            }
            return planes;
        }

        private static List<Brep> CutSlabs(Brep brep, List<Plane> planes, List<Plane> crossPlanes, double thickness, Vector3d side)
        {
            var tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            var slabs = new List<Brep>();

            foreach (var plane in planes)
            {
                try
                {
                    Curve[] crvs = new Curve[0];
                    Point3d[] pts = new Point3d[0];
                    if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, plane, tol, out crvs, out pts)) { continue; }
                    if (crvs == null) { continue; }
                    crvs = Curve.JoinCurves(crvs);
                    for (int i = 0; i < crvs.Length; i++)
                    {
                        var crv = crvs[i];
                        if (!crv.IsPlanar()) { Debug.WriteLine("crv not planar"); continue; }
                        if (!crv.IsClosed)
                        {
                            if (!crv.MakeClosed(tol))
                            {
                                Debug.WriteLine("crv not closed");
                                continue;
                            }
                        }

                        //add notches to curve
                        crv = NotchSlab(crv, crossPlanes, thickness, side);
                        if (!crv.IsPlanar()) { Debug.WriteLine("crv not planar 2"); continue; }
                        if (!crv.IsClosed)
                        {
                            if (!crv.MakeClosed(tol))
                            {
                                Debug.WriteLine("crv not closed 2");
                                continue;
                            }
                        }

                        Brep[] breps = Brep.CreatePlanarBreps(crv, tol);
                        if (breps == null) { Debug.WriteLine("breps null"); continue; }
                        foreach (var b in breps)
                        {
                            slabs.Add(b);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return slabs;
        }

        private static Curve NotchSlab(Curve crv, List<Plane> crossPlanes, double thickness, Vector3d side)
        {
            var tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //get plane to confirm direction
            Plane normalPlane = Plane.Unset;
            crv.TryGetPlane(out normalPlane);
            Vector3d normal = normalPlane.Normal;

            foreach (var plane in crossPlanes)
            {
                //create notch planes
                var xformPos = Transform.Translation(plane.Normal * (thickness / 2.00));
                var xformNeg = Transform.Translation(-plane.Normal * (thickness / 2.00));
                var planeOffPos = new Plane(plane);
                var planeOffNeg = new Plane(plane);
                planeOffPos.Transform(xformPos);
                planeOffNeg.Transform(xformNeg);

                //get intersections
                var xPos = Rhino.Geometry.Intersect.Intersection.CurvePlane(crv, planeOffPos, tol);
                var xNeg = Rhino.Geometry.Intersect.Intersection.CurvePlane(crv, planeOffNeg, tol);
                if (xPos == null || xNeg == null) { Debug.WriteLine("No cross intersection found"); continue; }
                if (xPos.Count == 0 || xNeg.Count == 0) { Debug.WriteLine("No cross intersection found"); continue; }
                if (xPos.Count < 2 || xNeg.Count < 2) { Debug.WriteLine("Not enough intersections found"); continue; }

                //get notch information
                double pPos;
                var lineNeg = GetNotchSide(xPos, side, out pPos);
                double pNeg;
                var linePos = GetNotchSide(xNeg, side, out pNeg);
                var lineMid = new Line(lineNeg.PointAt(1), linePos.PointAt(1));

                //split curve
                var splits = crv.Split(new List<double>() { pPos, pNeg });
                if (splits.Length < 2) { continue; }
                var tempCrv = splits[0];
                if (splits[0].GetLength() < splits[1].GetLength())
                {
                    tempCrv = splits[1];
                }

                //join notch with curve
                var join = Curve.JoinCurves(new List<Curve>() { tempCrv, lineNeg.ToNurbsCurve(), lineMid.ToNurbsCurve(), linePos.ToNurbsCurve() });
                if (join.Length == 0) { continue; } //if the join fails, we continue
                crv = join[0];
            }
            return crv;
        }

        private static Line GetNotchSide(Rhino.Geometry.Intersect.CurveIntersections x, Vector3d side, out double p)
        {
            //get parameter
            var start = x[0].PointA;
            var end = x[1].PointA;

            p = x[0].ParameterA;

            //determine if direction is correct
            var a = Vector3d.VectorAngle(side, start - end);
            var aR = Vector3d.VectorAngle(side, end - start);

            if (a > aR)
            {
                var tempS = new Point3d(start);
                var tempE = new Point3d(end);
                start = new Point3d(tempE);
                end = new Point3d(tempS);
                p = x[1].ParameterA; //update parameter if line is reversed
            }

            var mid = (start + end) / 2.00;
            var line = new Line(start, mid);

            return line;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Resource.AddTabs; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("D83D0688-1BE3-4C31-828D-0CF6F61793D8"); }
        }
    }
}
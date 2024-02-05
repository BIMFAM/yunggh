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
            var xSlabs = CutSlabs(brep, xPlanes, yPlanes, thickness);
            var ySlabs = CutSlabs(brep, yPlanes, xPlanes, thickness);

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

        private static List<Brep> CutSlabs(Brep brep, List<Plane> planes, List<Plane> crossPlanes, double thickness)
        {
            var tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            var slabs = new List<Brep>();

            foreach (var plane in planes)
            {
                try
                {
                    Curve[] crvs = new Curve[0];
                    Point3d[] pts = new Point3d[0];
                    if(!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, plane, tol, out crvs, out pts)) { continue; }
                    if (crvs == null) { continue; }
                    crvs = Curve.JoinCurves(crvs);
                    foreach (var crv in crvs)
                    {
                        if (!crv.IsPlanar()) { continue; }
                        if (!crv.IsClosed) { continue; }
                        Brep[] breps = Brep.CreatePlanarBreps(crv, 0.0001);
                        if (breps == null) { continue; }
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
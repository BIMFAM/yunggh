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
    public class MultiUnroll : GH_Component
    {
        public MultiUnroll()
          : base("Multi Unroll", "MultiUnroll",
              "Unroll multiple planar surfaces without duplicating edge points.",
              "yung gh", "Geometry")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Polysurface Panel Strips.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Unroll", "U", "Unroll Polysurfaces", GH_ParamAccess.tree);
            pManager.AddPointParameter("Point", "P", "Unrolled Points", GH_ParamAccess.tree);
            pManager.AddPointParameter("Point", "E", "Existing Points", GH_ParamAccess.tree);
            pManager.AddPointParameter("Center Point", "C", "Center Point", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            List<Brep> B = new List<Brep>();

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetDataList(0, B)) return;
            if(B.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Breps input."); return; }

            //constant
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //create unrolls
            GH_Structure<GH_Brep> unrolls = new GH_Structure<GH_Brep>();
            GH_Structure<GH_Point> unrollPts = new GH_Structure<GH_Point>();
            GH_Structure<GH_Point> existingPts = new GH_Structure<GH_Point>();
            GH_Structure<GH_Point> centers = new GH_Structure<GH_Point>();
            List<Point3d> existingTabs = new List<Point3d>(); //used not as output, but for checking if tab connection has already been made

            for (int j = 0; j < B.Count; j++)
            {
                //setup
                Brep brep = B[j];
                if(brep == null) { continue; }

                //guard statement to ignore single surfaces
                if (brep.Surfaces.Count < 2) { continue; }

                GH_Path path = new GH_Path(j);
                List<Point3d> unrolledPts = new List<Point3d>();
                List<Point3d> existing = new List<Point3d>();
                List<Point3d> vertices = new List<Point3d>();
                List<Brep> unrolledBreps = new List<Brep>();

                //create unroll
                Rhino.Geometry.Unroller unroll = null;
                if (brep != null)
                {
                    unroll = new Rhino.Geometry.Unroller(brep);
                }

                //guard statement
                if (unroll == null)
                {
                    //unrolls.Add(null,path);
                    //unrollPts.Add(null,path);
                    continue;
                }

                //unroll variables
                unroll.AbsoluteTolerance = tolerance;
                unroll.RelativeTolerance = tolerance;
                unroll.ExplodeOutput = false;

                Curve[] loops = brep.DuplicateNakedEdgeCurves(true, true);
                //get points for unroll
                foreach (Curve curve in loops)
                {
                    Polyline polyline;
                    if (!curve.TryGetPolyline(out polyline)) { continue; }

                    //get middle vertices
                    for (int i = 1; i < polyline.Count; i++)
                    {
                        Point3d s = polyline[i - 1];
                        Point3d e = polyline[i];
                        Point3d v = new Point3d((s.X + e.X) / 2.00, (s.Y + e.Y) / 2.00, (s.Z + e.Z) / 2.00);
                        vertices.Add(v);

                        //add tab points if not alread existing
                        bool alreadyExisting = false;
                        foreach (Point3d pt in existingTabs)
                        {
                            if (!(pt.DistanceTo(v) < 0.01)) { continue; }
                            alreadyExisting = true;
                            break;
                        }
                        if (alreadyExisting) { continue; }
                        existingTabs.Add(v);
                        existing.Add(v);
                        unroll.AddFollowingGeometry(v);
                    }
                }

                //add points to unroll

                //unroll
                Rhino.Geometry.Curve[] curves;
                Rhino.Geometry.Point3d[] points;
                Rhino.Geometry.TextDot[] dots;
                Rhino.Geometry.Brep[] breps = unroll.PerformUnroll(out curves, out points, out dots);

                breps = Rhino.Geometry.Brep.JoinBreps(breps, tolerance);
                if (breps == null) { continue; }

                //add to output
                foreach (Brep b in breps)
                {
                    unrolledBreps.Add(b);
                }
                foreach (Point3d pt in points)
                {
                    unrolledPts.Add(pt);
                }

                //get center
                Point3d center = YungGH.AveragePoint(vertices);
                center = brep.ClosestPoint(center);

                //convert to Grasshopper structure
                List<GH_Brep> unrolledBrepsGH = ConvertToGH(unrolledBreps);
                List<GH_Point> unrolledPtsGH = ConvertToGH(unrolledPts);
                List<GH_Point> existingGH = ConvertToGH(existing);
                List<GH_Point> centerGH = ConvertToGH(new List<Point3d>() { center });

                //add to tree
                unrolls.AppendRange(unrolledBrepsGH, path);
                unrollPts.AppendRange(unrolledPtsGH, path);
                existingPts.AppendRange(existingGH, path);
                centers.AppendRange(centerGH, path);
            }

            //TODO: create tabs

            //TODO: pack on sheets

            //output
            DA.SetDataTree(0, unrolls); //U
            DA.SetDataTree(1, unrollPts); //P
            DA.SetDataTree(2, existingPts); //E
            DA.SetDataTree(3, centers); //C
        }

        public static List<GH_Brep> ConvertToGH(IEnumerable<Brep> breps)
        {
            List<GH_Brep> brepsGH = new List<GH_Brep>();
            for (int i = 0; i < breps.Count(); i++)
            {
                Brep b = breps.ElementAt(i);
                GH_Brep ghB = null;
                if (!GH_Convert.ToGHBrep_Primary(b, ref ghB)) { continue; }
                brepsGH.Add(ghB);
            }
            return brepsGH;
        }

        public static List<GH_Point> ConvertToGH(List<Point3d> points)
        {
            List<GH_Point> pointsGH = new List<GH_Point>();
            for (int i = 0; i < points.Count; i++)
            {
                Point3d p = points[i];
                GH_Point ghP = null;
                if (!GH_Convert.ToGHPoint_Primary(p, ref ghP)) { continue; }
                pointsGH.Add(ghP);
            }
            return pointsGH;
        }

        public static List<GH_Curve> ConvertToGH(List<Curve> curves)
        {
            List<GH_Curve> curvesGH = new List<GH_Curve>();
            for (int i = 0; i < curves.Count; i++)
            {
                Curve c = curves[i];
                GH_Curve ghC = new GH_Curve();
                if (!GH_Convert.ToGHCurve_Primary(c, ref ghC)) { continue; }
                curvesGH.Add(ghC);
            }
            return curvesGH;
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("F5BAB653-888D-4D29-8619-27D43738E87C"); }
        }
    }
}
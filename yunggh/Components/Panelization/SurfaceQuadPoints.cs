using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class SurfaceQuadPoints : GH_Component
    {
        #region UI

        public SurfaceQuadPoints()
          : base("Surface Quad Corners", "Quads",
              "Get quad corners for a surface.",
              "yung gh", "Panelization")
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8D9E1853-85F3-4ADA-93DB-769AA775C537"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("U Curves", "U", "'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("V Curves", "V", "'V' Curves", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Flip U Curves", "FU", "Flip 'U' Curves", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip V Curves", "FV", "Flip 'V' Curves", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Quad Points", "P", "Data tree with Quad Points", GH_ParamAccess.tree);
        }

        #endregion UI

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //input
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var uFlip = false;
            var vFlip = false;
            if (!DA.GetDataList(0, uCrvs)) return;
            if (!DA.GetDataList(1, vCrvs)) return;
            if (!DA.GetData(2, ref uFlip)) return;
            if (!DA.GetData(3, ref vFlip)) return;

            //main process
            var uCrvsSorted = new List<Curve>();
            var vCrvsSorted = new List<Curve>();
            var uIndicesSorted = new List<int>();
            var vIndicesSorted = new List<int>();
            SortCurvesBySurface.Sort(uCrvs, vCrvs, uFlip, vFlip
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //get quad corners
            var quads = GetQuadCorners(uCrvsSorted, vCrvsSorted);

            //output
            var quadTree = ToDataTree(quads);
            DA.SetDataTree(0, quadTree);
        }

        public static List<List<List<Point3d>>> GetQuadCorners(List<Curve> uCrvs, List<Curve> vCrvs)
        {
            //setting up a complete grid with empty point values
            var pts = new List<List<Point3d>>();
            for (int u = 0; u < uCrvs.Count; u++)
            {
                var crvPts = new List<Point3d>();
                for (int v = 0; v < vCrvs.Count; v++)
                {
                    crvPts.Add(Point3d.Unset);
                }
                pts.Add(crvPts);
            }

            //getting all curve intersection points
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            for (int u = 0; u < pts.Count; u++)
            {
                var uCrv = uCrvs[u];
                for (int v = 0; v < vCrvs.Count; v++)
                {
                    var vCrv = vCrvs[v];
                    var crvXs = Rhino.Geometry.Intersect.Intersection.CurveCurve(uCrv, vCrv, tol, tol);
                    if (crvXs.Count == 0) { continue; }
                    foreach (var crvX in crvXs) { pts[u][v] = crvX.PointA; }
                }
            }

            //find end points that might not have been an intersection
            int[] setsS = new int[pts.Count];
            int[] setsE = new int[pts.Count];
            for (int u = 1; u < pts.Count - 1; u++)
            {
                //Debug.WriteLine("A");
                var uCrv = uCrvs[u - 1];
                //Debug.WriteLine("B");

                for (int v = 1; v < vCrvs.Count - 1; v++)
                {
                    //Debug.WriteLine("C" + u + "-" + v);
                    var vCrv = vCrvs[v - 1];
                    //Debug.WriteLine("D" + u + "-" + v);
                    var pt = pts[u][v];
                    //Debug.WriteLine("E" + u + "-" + v);
                    if (pt == Point3d.Unset) { continue; }

                    //V
                    var ptVS = pts[u][v - 1];
                    if (ptVS == Point3d.Unset && setsS[u] < 1)
                    {
                        //Debug.WriteLine("H" + u + "-" + v);
                        var start = vCrv.PointAtStart;
                        var end = vCrv.PointAtEnd;
                        if (start.DistanceTo(pt) > tol && end.DistanceTo(pt) > tol)
                        {
                            if (pt.DistanceTo(start) < pt.DistanceTo(end))
                                pts[u][v - 1] = start;
                            else
                                pts[u][v - 1] = end;
                            setsS[u]++;
                        }
                        //Debug.WriteLine("I" + u + "-" + v);
                    }
                    var ptVE = pts[u][v + 1];
                    if (ptVE == Point3d.Unset && setsE[u] < 1)
                    {
                        //Debug.WriteLine("L" + u + "-" + v);
                        var start = vCrv.PointAtStart;
                        var end = vCrv.PointAtEnd;
                        if (start.DistanceTo(pt) > tol && end.DistanceTo(pt) > tol)
                        {
                            if (pt.DistanceTo(start) < pt.DistanceTo(end))
                                pts[u][v + 1] = start;
                            else
                                pts[u][v + 1] = end;
                            setsE[u]++;
                        }
                        //Debug.WriteLine("M" + u + "-" + v);
                    }

                    //U
                    var ptUS = pts[u - 1][v];
                    if (ptUS == Point3d.Unset && setsS[u - 1] < 1)
                    {
                        //Debug.WriteLine("F" + u + "-" + v);
                        var start = uCrv.PointAtStart;
                        var end = uCrv.PointAtEnd;
                        if (start.DistanceTo(pt) > tol && end.DistanceTo(pt) > tol)
                        {
                            if (pt.DistanceTo(start) < pt.DistanceTo(end))
                                pts[u - 1][v] = start;
                            else
                                pts[u - 1][v] = end;
                            setsS[u - 1]++;
                        }
                        //Debug.WriteLine("G" + u + "-" + v);
                    }
                    var ptUE = pts[u + 1][v];
                    if (ptUE == Point3d.Unset && setsE[u + 1] < 1)
                    {
                        //Debug.WriteLine("J" + u + "-" + v);
                        var start = uCrv.PointAtStart;
                        var end = uCrv.PointAtEnd;
                        if (start.DistanceTo(pt) > tol && end.DistanceTo(pt) > tol)
                        {
                            if (pt.DistanceTo(start) < pt.DistanceTo(end))
                                pts[u + 1][v] = start;
                            else
                                pts[u + 1][v] = end;
                            setsE[u + 1]++;
                        }
                        //Debug.WriteLine("K" + u + "-" + v);
                    }
                }
            }

            //grouping points into quads
            var output = new List<List<List<Point3d>>>();
            for (int i = 1; i < pts.Count; i++)
            {
                var row = new List<List<Point3d>>();

                var pts0 = pts[i - 1];
                var pts1 = pts[i];

                for (int j = 1; j < pts0.Count; j++)
                {
                    var pt0 = pts0[j - 1];
                    var pt1 = pts0[j];
                    var pt2 = pts1[j];
                    var pt3 = pts1[j - 1];

                    var quad = new List<Point3d>() { pt0, pt1, pt2, pt3 };
                    row.Add(quad);
                }
                output.Add(row);
            }

            return output;
        }

        public static GH_Structure<GH_Point> ToDataTree(List<List<List<Point3d>>> listListList)
        {
            var dataTree = new GH_Structure<GH_Point>();

            for (int i = 0; i < listListList.Count; i++)
            {
                var listList = listListList[i];
                for (int j = 0; j < listList.Count; j++)
                {
                    var list = listList[j];
                    var goos = new List<GH_Point>();
                    for (int k = 0; k < list.Count; k++)
                    {
                        var obj = list[k];
                        GH_Point target = null;
                        GH_Convert.ToGHPoint_Primary(obj, ref target);
                        goos.Add(target);
                    }
                    dataTree.AppendRange(goos, new GH_Path(i, j));
                }
            }

            return dataTree;
        }
    }
}
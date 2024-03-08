using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class PyramidPanelization : GH_Component
    {
        #region UI

        public PyramidPanelization()
          : base("Pyramid Panelization", "PYRPNL",
              "Panelize Surface with Pyramid type panelization method.",
              "yung gh", "Panelization")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("D1F8782F-F468-4C25-AB97-B7791C587C83"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Surface", "S", "Panelization Surface (can be double curved)", GH_ParamAccess.item);
            pManager.AddCurveParameter("U Curves", "U", "'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("V Curves", "V", "'V' Curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Height", "H", "Pyramid Height", GH_ParamAccess.item);
            pManager.AddNumberParameter("Truncation", "T", "Normalized Pyramid Truncation Location", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip U Curves", "FU", "Flip 'U' Curves", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip V Curves", "FV", "Flip 'V' Curves", GH_ParamAccess.item, false);
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Pyramid Panels", "P", "Pyramid Panels", GH_ParamAccess.list);
        }

        #endregion UI

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //inputs
            var brep = new Brep();
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var height = 1.0;
            var truncation = 1.0;
            var uFlip = false;
            var vFlip = false;
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, uCrvs)) return;
            if (!DA.GetDataList(2, vCrvs)) return;
            if (!DA.GetData(3, ref height)) return;
            DA.GetData(4, ref truncation);
            DA.GetData(5, ref uFlip);
            DA.GetData(6, ref vFlip);

            //main
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
            var quads = SurfaceQuadPoints.GetQuadCorners(uCrvsSorted, vCrvsSorted);

            //create pyramid panels
            var panels = GetPyramidPanels(quads, brep, height, truncation);

            //output
            DA.SetDataList(0, panels);
        }

        public static List<Brep> GetPyramidPanels(List<List<List<Point3d>>> quadsByRow, Brep brep, double height, double truncation)
        {
            var output = new List<Brep>();

            for (int r = 0; r < quadsByRow.Count; r++)
            {
                var row = quadsByRow[r];
                for (int j = 0; j < row.Count; j++)
                {
                    var quad = row[j];
                    if (quad[0] == Point3d.Unset || quad[1] == Point3d.Unset || quad[2] == Point3d.Unset || quad[3] == Point3d.Unset)
                    {
                        //find out how many points are unset
                        int unsetPtCount = 0;
                        foreach (var pt in quad)
                        {
                            if (pt != Point3d.Unset) { continue; }
                            unsetPtCount++;
                        }
                        if (unsetPtCount > 1) { continue; } //continue if not enough points for triangle

                        //create single quad triangle panel
                        //Debug.WriteLine("D" + r + "-" + j);
                        if (quad[0] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[1], quad[2], quad[3]);
                            if (pnl == null) { continue; }
                            output.Add(pnl.ToBrep());
                        }
                        else if (quad[1] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[2], quad[3]);
                            if (pnl == null) { continue; }
                            output.Add(pnl.ToBrep());
                        }
                        else if (quad[2] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[1], quad[3]);
                            if (pnl == null) { continue; }
                            output.Add(pnl.ToBrep());
                        }
                        else if (quad[3] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[1], quad[2]);
                            if (pnl == null) { continue; }
                            output.Add(pnl.ToBrep());
                        }
                        //Debug.WriteLine("C" + r + "-" + j);
                        continue; //continue because quad is triangle
                    }

                    //get pyramid tip
                    var center = (quad[0] + quad[1] + quad[2] + quad[3]) / 4.00;
                    var srf = brep.Surfaces[0];
                    double u, v;
                    srf.ClosestPoint(center, out u, out v);
                    var normal = srf.NormalAt(u, v);
                    center += normal * height;

                    //create pyramid panels
                    for (int i = 1; i <= quad.Count; i++)
                    {
                        var pt0 = quad[i - 1];
                        var pt1 = Point3d.Unset;
                        if (i == quad.Count)
                        {
                            pt1 = quad[0];
                        }
                        else
                        {
                            pt1 = quad[i];
                        }
                        //Debug.WriteLine("A" + r + "-" + j + "-" + i);
                        var pnl = NurbsSurface.CreateFromCorners(pt0, pt1, center);
                        if (pnl == null) { continue; }
                        //Debug.WriteLine("B");
                        output.Add(pnl.ToBrep());
                        //Debug.WriteLine("C");
                    }
                }
            }

            return output;
        }
    }
}
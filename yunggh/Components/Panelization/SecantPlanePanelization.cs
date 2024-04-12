using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class SecantPlanePanelization : GH_Component
    {
        #region UI

        public SecantPlanePanelization()
          : base("Secant Plane Panelization", "SECPNL",
              "Panelize Surface with Secant Plane type panelization method.",
              "yung gh", "Panelization")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("EEA8B608-19AF-47FA-8E65-EC2E98BB9853"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Surface", "S", "Panelization Surface (can be double curved)", GH_ParamAccess.item);
            pManager.AddCurveParameter("U Curves", "U", "'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("V Curves", "V", "'V' Curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Pointiness", "P", "Pulled Corner Location Offset", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Pulled Point Logic", "L", "Determine which point to pull", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Flip U Curves", "FU", "Flip 'U' Curves", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip V Curves", "FV", "Flip 'V' Curves", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Secant Plane Panels", "P", "Pyramid Panels", GH_ParamAccess.list);
        }

        #endregion UI

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //inputs
            var brep = new Brep();
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var pulledPointPointiness = 0.0;
            var pulledPointType = 0;
            var uFlip = false;
            var vFlip = false;
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, uCrvs)) return;
            if (!DA.GetDataList(2, vCrvs)) return;
            DA.GetData(3, ref pulledPointPointiness);
            DA.GetData(4, ref pulledPointType);
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
            var panels = GetSecantPlanePanels(quads, brep, pulledPointPointiness, pulledPointType);

            //output
            DA.SetDataList(0, panels);
        }

        public static List<Brep> GetSecantPlanePanels(List<List<List<Point3d>>> quadsByRow, Brep brep, double pulledPointPointiness, int pulledPointType)
        {
            var output = new List<Brep>();

            for (int r = 0; r < quadsByRow.Count; r++)
            {
                var row = quadsByRow[r];
                for (int j = 0; j < row.Count; j++)
                {
                    var quad = row[j];
                    //TODO: create inheritance class
                    //test if quad is a triangle
                    if (quad[0] == Point3d.Unset || quad[1] == Point3d.Unset || quad[2] == Point3d.Unset || quad[3] == Point3d.Unset)
                    {
                        continue; //continue because quad is triangle
                    }

                    //get pulled point
                    if (pulledPointType < 4)
                    {
                        var originaPulledPoint = quad[pulledPointType];
                        var quadByType = quad.ToList();

                        //create plane and project Pulled Point
                        var plane = new Plane(quadByType[1], quadByType[2], quadByType[3]);
                        var pulledPoint = plane.ClosestPoint(originaPulledPoint);
                        //create quad
                        var quadPnl = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[1], quadByType[2], quadByType[3]);

                        //create triangles
                        var trPnl1 = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[1], originaPulledPoint);
                        var trPnl2 = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[3], originaPulledPoint);
                        output.Add(quadPnl.ToBrep());
                        output.Add(trPnl1.ToBrep());
                        output.Add(trPnl2.ToBrep());
                    }
                }
            }

            return output;
        }
    }
}
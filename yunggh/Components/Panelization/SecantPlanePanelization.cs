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
            pManager.AddVectorParameter("Pointiness", "V", "Pulled Corner Location Offset as Vector (uses X,Y).", GH_ParamAccess.item, Vector3d.Zero);
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
            var pulledPointPointiness = Vector3d.Zero;
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

        public static List<Point3d> OrderQuadByPulledPoint(List<Point3d> quad, int pulledPointType)
        {
            //get variables
            var originaPulledPoint = new Point3d(quad[pulledPointType]);
            var quadByType = quad.ToList();

            var pt0 = new Point3d(quad[0]);
            var pt1 = new Point3d(quad[1]);
            var pt2 = new Point3d(quad[2]);
            var pt3 = new Point3d(quad[3]);

            //assume 0 as the default, only check things above that
            if (pulledPointType == 1)
            {
                quadByType[0] = pt1;
                quadByType[1] = pt2;
                quadByType[2] = pt3;
                quadByType[3] = pt0;
            }
            else if (pulledPointType == 2)
            {
                quadByType[0] = pt2;
                quadByType[1] = pt3;
                quadByType[2] = pt0;
                quadByType[3] = pt1;
            }
            else if (pulledPointType == 3)
            {
                quadByType[0] = pt3;
                quadByType[1] = pt0;
                quadByType[2] = pt1;
                quadByType[3] = pt2;
            }

            //add original point and return
            quadByType[4] = originaPulledPoint;
            return quadByType;
        }

        public static List<Brep> GetSecantPlanePanels(List<List<List<Point3d>>> quadsByRow, Brep brep, Vector3d pulledPointPointiness, int pulledPointType)
        {
            //guard statements
            if (pulledPointType > 3) { pulledPointType = 0; }
            if (pulledPointType < 0) { pulledPointType = 0; }

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

                    //order quad by pulledpoint type
                    var quadByType = OrderQuadByPulledPoint(quad, pulledPointType);

                    //create plane and project Pulled Point
                    var plane = new Plane(quadByType[1], quadByType[2], quadByType[3]);
                    var pulledPoint = plane.ClosestPoint(quadByType[0]);

                    //apply pulled point Pointiness
                    var moveVec = plane.XAxis * pulledPointPointiness.X + plane.YAxis * pulledPointPointiness.Y;
                    var xform = Transform.Translation(moveVec);
                    pulledPoint.Transform(xform);

                    //create quad
                    var quadPnl = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[1], quadByType[2], quadByType[3]);

                    //create triangles
                    var trPnl1 = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[1], quadByType[4]);
                    var trPnl2 = NurbsSurface.CreateFromCorners(pulledPoint, quadByType[3], quadByType[4]);
                    output.Add(quadPnl.ToBrep());
                    output.Add(trPnl1.ToBrep());
                    output.Add(trPnl2.ToBrep());
                }
            }

            return output;
        }
    }
}
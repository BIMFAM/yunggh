using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class SecantPlanePanelization : PanelizationBase
    {
        #region UI

        public SecantPlanePanelization()
          : base("Secant Plane Panelization", "SECPNL", "Panelize Surface with Secant Plane type panelization method.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("EEA8B608-19AF-47FA-8E65-EC2E98BB9853"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddVectorParameter("Pointiness", "V", "Pulled Corner Location Offset as Vector (uses X,Y).", GH_ParamAccess.item, Vector3d.Zero);
            pManager.AddIntegerParameter("Pulled Point Logic", "L", "Determine which point to pull", GH_ParamAccess.item, 0);
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        #endregion UI

        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            //get variables
            var pulledPointPointiness = Vector3d.Zero;
            var pulledPointType = 0;
            DA.GetData(5, ref pulledPointPointiness);
            DA.GetData(6, ref pulledPointType);

            //create secant panels
            var panels = GetSecantPlanePanels(quads, brep, pulledPointPointiness, pulledPointType);

            return panels;
        }

        public static List<Point3d> OrderQuadByPulledPoint(List<Point3d> quad, int pulledPointType)
        {
            //find lowest point and use that as pulled point index
            if (pulledPointType == 4)
            {
                var lowestZ = double.PositiveInfinity;
                for (int i = 0; i < quad.Count; i++)
                {
                    if (quad[i] == Point3d.Unset) { continue; }
                    if (lowestZ < quad[i].Z) { continue; }
                    lowestZ = quad[i].Z;
                    pulledPointType = i;
                }
            }
            else if (pulledPointType == 5)
            {
                var highestZ = double.NegativeInfinity;
                for (int i = 0; i < quad.Count; i++)
                {
                    if (quad[i] == Point3d.Unset) { continue; }
                    if (highestZ > quad[i].Z) { continue; }
                    highestZ = quad[i].Z;
                    pulledPointType = i;
                }
            }

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

        public static List<List<List<Brep>>> GetSecantPlanePanels(List<List<List<Point3d>>> quadsByRow, Brep brep, Vector3d pulledPointPointiness, int pulledPointType)
        {
            //guard statements
            if (pulledPointType > 5) { pulledPointType = 0; }
            if (pulledPointType < 0) { pulledPointType = 0; }

            var allOutput = new List<List<List<Brep>>>();

            for (int r = 0; r < quadsByRow.Count; r++)
            {
                var rowOutput = new List<List<Brep>>();
                var row = quadsByRow[r];
                for (int j = 0; j < row.Count; j++)
                {
                    var panels = new List<Brep>();
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
                    panels.Add(quadPnl.ToBrep());
                    panels.Add(trPnl1.ToBrep());
                    panels.Add(trPnl2.ToBrep());
                    rowOutput.Add(panels);
                }
                allOutput.Add(rowOutput);
            }

            return allOutput;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return ListListListToTree(panels);
        }
    }
}
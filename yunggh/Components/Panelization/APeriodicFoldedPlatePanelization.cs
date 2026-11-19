using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Render.ChangeQueue;
using Rhino.Runtime.RhinoAccounts;

namespace yunggh.Components.Panelization
{
    public class APeriodicFoldedPlatePanelization : PanelizationBase
    {
        #region UI

        public APeriodicFoldedPlatePanelization()
          : base("APeriodic Folded Plate Panelization", "AFPPNL", "Panelize Surface with Aperiodic Folded Plate type panelization method.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("B8A10BDC-181C-4320-B211-0C28793C8B30"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddNumberParameter("Offset", "O", "Offset Distance", GH_ParamAccess.item, 10.0);
            pManager.AddIntegerParameter("Type", "T", "Offset Type", GH_ParamAccess.item, 1);
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        #endregion UI

        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            //get inputs
            var offset = 10.0;
            var offsetType = 1;
            DA.GetData(5, ref offset);
            DA.GetData(6, ref offsetType);

            //get the point grid the same way the original definition does:
            //the intersections of the U curves with the V curves.
            //NOTE: the quad corners can't be used here, they contain Point3d.Unset
            //and substituted curve end points, which led to failed plane intersections
            //and ridge points being placed at the world origin
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var uFlip = false;
            var vFlip = false;
            DA.GetDataList(1, uCrvs);
            DA.GetDataList(2, vCrvs);
            DA.GetData(3, ref uFlip);
            DA.GetData(4, ref vFlip);

            var uCrvsSorted = new List<Curve>();
            var vCrvsSorted = new List<Curve>();
            var uIndicesSorted = new List<int>();
            var vIndicesSorted = new List<int>();
            SortCurvesBySurface.Sort(uCrvs, vCrvs, false, false
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //curves can be broken into pieces by holes or trims, so points are placed by the
            //grid line they are on, with Point3d.Unset where the lines don't meet
            var pointRows = SurfaceGrid.Build(uCrvsSorted, vCrvsSorted).PointGrid();
            if (uFlip) { pointRows.Reverse(); }
            if (vFlip) { foreach (var row in pointRows) { row.Reverse(); } }

            //create aperiodic panels
            var panels = GetAperiodicPanels(pointRows, brep.Surfaces[0], offset, offsetType);

            return panels;
        }

        private List<List<List<Brep>>> GetAperiodicPanels(List<List<Point3d>> pointRows, Surface SRF, double offset, int type)
        {
            //if(T == null){T = 1;}

            var panels = new List<List<List<Brep>>>();
            for (int p = 1; p < pointRows.Count; p++)
            {
                //get inputs
                List<Point3d> pts0 = pointRows[p - 1];
                List<Point3d> pts1 = pointRows[p];

                if (pts0.Count < 2 || pts1.Count < 2) { continue; }

                //get point counts
                int ptCount = pts0.Count;
                if (pts1.Count < ptCount) { ptCount = pts1.Count; }

                //the ridge point is started at the first cell and again after every gap
                Point3d sR = Point3d.Unset;

                //setup outputs
                var panelRow0 = new List<List<Brep>>();
                var panelRow1 = new List<List<Brep>>();

                //construct all the panels
                for (int i = 1; i < ptCount; i++)
                {
                    var panelCell0 = new List<Brep>();
                    var panelCell1 = new List<Brep>();
                    //get points
                    Point3d s0 = pts0[i - 1];
                    Point3d e0 = pts0[i];
                    Point3d s1 = pts1[i - 1];
                    Point3d e1 = pts1[i];

                    //skip if any corner is missing (hole or outside the trim)
                    if (!s0.IsValid || !e0.IsValid || !s1.IsValid || !e1.IsValid)
                    {
                        panelRow0.Add(panelCell0);
                        panelRow1.Add(panelCell1);
                        sR = Point3d.Unset; //don't carry the ridge across the gap
                        continue;
                    }

                    //get initial ridge point
                    if (!sR.IsValid) { sR = GetFirstRidgePoint(s0, s1, SRF, offset); }

                    //construct second ridge point
                    Point3d eR = GetSecondRidgePoint(sR, s0, e0, s1, e1, SRF, type);

                    //create surfaces
                    NurbsSurface srfPt0 = NurbsSurface.CreateFromCorners(s0, sR, eR, e0);
                    NurbsSurface srfPt1 = NurbsSurface.CreateFromCorners(sR, s1, e1, eR);

                    //add to output
                    if (!(srfPt0 is null))
                    {
                        panelCell0.Add(srfPt0.ToBrep());
                    }
                    if (!(srfPt1 is null))
                    {
                        panelCell1.Add(srfPt1.ToBrep());
                    }
                    panelRow0.Add(panelCell0);
                    panelRow1.Add(panelCell1);
                    //update ridge point for next set of points
                    sR = eR;
                }

                //set output
                panels.Add(panelRow0);
                panels.Add(panelRow1);
            }
            return panels;
        }

        private static Point3d GetFirstRidgePoint(Point3d s0, Point3d s1, Surface SRF, double offset)
        {
            Point3d sR = (s0 + s1) / 2.00;
            double u; double v;
            SRF.ClosestPoint(sR, out u, out v);
            Vector3d normal = SRF.NormalAt(u, v);
            sR.Transform(Transform.Translation(normal * offset));
            return sR;
        }

        public Point3d GetSecondRidgePoint(Point3d rS, Point3d s0, Point3d e0, Point3d s1, Point3d e1, Surface surface, int type)
        {
            //mid point between end points
            double x = (e0.X + e1.X) / 2.00;
            double y = (e0.Y + e1.Y) / 2.00;
            double z = (e0.Z + e1.Z) / 2.00;
            Point3d mid = new Point3d(x, y, z);
            Point3d third = mid;

            //fallback when the ridge can't be solved (degenerate or parallel planes):
            //carry the ridge across parallel to the edge instead of returning
            //an uninitialized line, which would put the point at the world origin
            Point3d startMid = (s0 + s1) / 2.0;
            Point3d fallback = rS + (mid - startMid);

            //setup planes
            Plane pln0 = new Plane(rS, s0, e0);
            Plane pln1 = new Plane(rS, s1, e1);
            if (!pln0.IsValid || !pln1.IsValid) { return fallback; }
            Line line;
            if (!Rhino.Geometry.Intersect.Intersection.PlanePlane(pln0, pln1, out line) || !line.IsValid)
            {
                return fallback;
            }

            //create plane using surface normal
            double u; double v;
            surface.ClosestPoint(mid, out u, out v);
            Vector3d normal = surface.NormalAt(u, v);
            Transform xform = Transform.Translation(normal);
            third.Transform(xform);
            Plane pln2 = new Plane(e0, e1, third);
            double p;
            if (pln2.IsValid && Rhino.Geometry.Intersect.Intersection.LinePlane(line, pln2, out p))
            {
                third = line.PointAt(p);
            }

            //get point
            Point3d rE = line.ClosestPoint(e0, false);
            if (type == 2)
            {
                rE = line.ClosestPoint(mid, false);
            }
            if (type == 3)
            {
                rE = line.ClosestPoint(third, false);
            }
            if (!rE.IsValid) { return fallback; }
            return rE;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return ListListListToTree(panels);
        }
    }
}
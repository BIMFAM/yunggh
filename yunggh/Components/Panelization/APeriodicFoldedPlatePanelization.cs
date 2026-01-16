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

            //create pyramid panels
            var panels = GetAperiodicPanels(quads, brep.Surfaces[0], offset, offsetType);

            return panels;
        }

        private List<List<Point3d>> GetPointRows(List<List<List<Point3d>>> quads)
        {
            //convert quads into rows of points
            var pointRows = new List<List<Point3d>>();
            for (int i = 1; i < quads.Count; i++)
            {
                var row = new List<Point3d>();
                var quadsRow = quads[i];
                for (int j = 1; j < quadsRow.Count; j++) //for each quad in quadrow
                {
                    row.Add(quadsRow[j][0]);
                }
                //row.Add(quadsRow[quadsRow.Count - 1][3]);

                //add to output
                pointRows.Add(row);
            }

            /*/
            //get last row
            var lastRow = new List<Point3d>();
            var lastQuadRow = quads[quads.Count - 1];
            foreach (var quad in lastQuadRow) //for each quad in quadrow
            {
                lastRow.Add(quad[1]);
            }
            lastRow.Add(lastQuadRow[lastQuadRow.Count - 1][2]);
            pointRows.Add(lastRow); //add to output
            //*/
            return pointRows;
        }

        private List<List<List<Brep>>> GetAperiodicPanels(List<List<List<Point3d>>> quads, Surface SRF, double offset, int type)
        {
            //if(T == null){T = 1;}

            var pointRows = GetPointRows(quads);

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

                //get initial ridge point
                double x = (pts0[0].X + pts1[0].X) / 2.00;
                double y = (pts0[0].Y + pts1[0].Y) / 2.00;
                double z = (pts0[0].Z + pts1[0].Z) / 2.00;
                Point3d sR = new Point3d(x, y, z);
                double u; double v;
                SRF.ClosestPoint(sR, out u, out v);
                Vector3d normal = SRF.NormalAt(u, v);
                normal = normal * offset;
                Transform xform = Transform.Translation(normal);
                sR.Transform(xform);

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

        public Point3d GetSecondRidgePoint(Point3d rS, Point3d s0, Point3d e0, Point3d s1, Point3d e1, Surface surface, int type)
        {
            //setup planes
            Plane pln0 = new Plane(rS, s0, e0);
            Plane pln1 = new Plane(rS, s1, e1);
            Line line;
            Rhino.Geometry.Intersect.Intersection.PlanePlane(pln0, pln1, out line);

            //mid point between end points
            double x = (e0.X + e1.X) / 2.00;
            double y = (e0.Y + e1.Y) / 2.00;
            double z = (e0.Z + e1.Z) / 2.00;
            Point3d mid = new Point3d(x, y, z);
            Point3d third = mid;

            //create plane using surface normal
            double u; double v;
            surface.ClosestPoint(mid, out u, out v);
            Vector3d normal = surface.NormalAt(u, v);
            Transform xform = Transform.Translation(normal);
            third.Transform(xform);
            Plane pln2 = new Plane(e0, e1, third);
            double p;
            Rhino.Geometry.Intersect.Intersection.LinePlane(line, pln2, out p);
            third = line.PointAt(p);

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
            return rE;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return ListListListToTree(panels);
        }
    }
}
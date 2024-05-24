using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class TangentPlanePanelization : PanelizationBase
    {
        #region UI

        public TangentPlanePanelization()
          : base("Tangent Plane Panelization", "TANPNL", "Panelize Surface with Tangent Plane type panelization method.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("113DA587-B9C7-4D50-85CB-367C3110D938"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
        }

        #endregion UI

        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            //create secant panels
            var panels = GetTangentPlanePanels(quads, brep);

            return panels;
        }

        public static List<List<List<Brep>>> GetTangentPlanePanels(List<List<List<Point3d>>> quadsByRow, Brep brep)
        {
            //get plane representation for each quad

            //split each panel using it's neighbor's planes
            var allOutput = new List<List<List<Brep>>>();
            for (int u = 1; u < quadsByRow.Count - 1; u++)
            {
                var preRow = quadsByRow[u - 1];
                var curRow = quadsByRow[u];
                var nextRow = quadsByRow[u + 1];

                var outputRow = new List<List<Brep>>();

                for (int v = 1; v < curRow.Count - 1; v++)
                {
                    var quad = curRow[v];

                    var neighborQuads = new List<List<Point3d>>();
                    //previous row
                    neighborQuads.Add(preRow[v - 1]);
                    neighborQuads.Add(preRow[v]);
                    neighborQuads.Add(preRow[v + 1]);

                    //next row
                    neighborQuads.Add(nextRow[v - 1]);
                    neighborQuads.Add(nextRow[v]);
                    neighborQuads.Add(nextRow[v + 1]);

                    //current row
                    neighborQuads.Add(curRow[v - 1]);
                    neighborQuads.Add(curRow[v + 1]);

                    //create planes
                    var inputPlane = PlaneFromQuad(quad, brep);
                    var clippingPlanes = new List<Plane>();
                    foreach (var q in neighborQuads)
                    {
                        var clippingPlane = PlaneFromQuad(q, brep);
                        if(clippingPlane == Plane.Unset) { continue; }
                        clippingPlanes.Add(clippingPlane);
                    }
                    if(clippingPlanes.Count == 0) 
                    { 
                        outputRow.Add(new List<Brep>() {});
                        continue;
                    }

                    //create panel
                    var pnl = ClipPlane(inputPlane, clippingPlanes);

                    outputRow.Add(new List<Brep>() { pnl });
                }

                allOutput.Add(outputRow);
            }

            return allOutput;
        }

        private static Plane PlaneFromQuad(List<Point3d> quad, Brep brep)
        {
            double count = 0;
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Point3d p in quad)
            {
                if (p == Point3d.Unset) { continue; }
                x += p.X;
                y += p.Y;
                z += p.Z;
                count++;
            }
            if(count == 0) { return Plane.Unset; }

            var center = new Point3d(x / count, y / count, z / count);
            Point3d cp;
            double s, c;
            Vector3d normal;
            ComponentIndex ci;
            brep.ClosestPoint(center, out cp, out ci, out s, out c, double.MaxValue, out normal);

            return new Plane(center, normal);
        }

        private static Brep ClipPlane(Plane inputPlane, List<Plane> clippingPlanes)
        {
            //find panel size
            double size = double.NegativeInfinity; //also holds distance
            Point3d origin = inputPlane.Origin;
            foreach (Plane pln in clippingPlanes)
            {
                double dist = origin.DistanceTo(pln.Origin);
                if (dist < size) { continue; }

                size = dist;
            }

            //create intial panel
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Interval interval = new Interval(-size, size);
            Interval intervalCutter = new Interval(-size * 10, size * 10);
            Rectangle3d rect = new Rectangle3d(inputPlane, interval, interval);
            Brep panel = Brep.CreatePlanarBreps(rect.ToNurbsCurve(), tol)[0];

            //Cut initial panel with each clipping Plane
            List<Brep> cutters = new List<Brep>();
            foreach (Plane pln in clippingPlanes)
            {
                Rectangle3d rectCutter = new Rectangle3d(pln, intervalCutter, intervalCutter);
                Brep cutter = Brep.CreatePlanarBreps(rectCutter.ToNurbsCurve(), tol)[0];
                cutters.Add(cutter);
            }
            Brep[] cuts = panel.Split(cutters, tol);

            double panelDist = double.PositiveInfinity;
            foreach (Brep cut in cuts)
            {
                Point3d cp = cut.ClosestPoint(origin);
                double dist = origin.DistanceTo(cp);
                if (dist > panelDist) { continue; }
                panelDist = dist;
                panel = cut;
            }

            return panel;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return ListListListToTree(panels);
        }
    }
}
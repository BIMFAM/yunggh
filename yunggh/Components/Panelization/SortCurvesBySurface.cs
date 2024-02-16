using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class SortCurvesBySurface : GH_Component
    {
        #region UI

        public SortCurvesBySurface()
          : base("Sort Curves By Surface", "SortCRVBySRF",
              "Sort Curves along 'U' and 'V' by a surface",
              "yung gh", "Panelization")
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B83B22FF-B6BE-47E8-AF91-F3C4DB00E431"); }
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
            pManager.AddCurveParameter("Sorted U Curves", "U", "Sorted 'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("Sorted V Curves", "V", "Sorted 'V' Curves", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Sorted U Curve Indicies", "UI", "Sorted 'U' Curves Indices", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Sorted V Curve Indicies", "VI", "Sorted 'V' Curves Indices", GH_ParamAccess.list);
        }

        #endregion UI

        protected override void SolveInstance(IGH_DataAccess DA)
        {
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
            Sort(uCrvs, vCrvs, uFlip, vFlip
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //output
            DA.SetDataList(0, uCrvsSorted);
            DA.SetDataList(1, vCrvsSorted);
            DA.SetDataList(2, uIndicesSorted);
            DA.SetDataList(3, vIndicesSorted);

            //setup preview
            _previewUCurves = uCrvsSorted;
            _previewVCurves = vCrvsSorted;
        }

        public static void Sort(List<Curve> U, List<Curve> V, bool FU, bool FV,
            ref List<Curve> UO, ref List<int> UI, ref List<Curve> VO, ref List<int> VI)
        {
            //guard statement
            if (U.Count == 0 || V.Count == 0) { return; }

            //initialize indices list
            List<int> iU = new List<int>();
            List<int> iV = new List<int>();
            int c = 0;
            foreach (Curve curve in U)
            {
                iU.Add(c);
                c++;
            }
            c = 0;
            foreach (Curve curve in V)
            {
                iV.Add(c);
                c++;
            }

            //get longest curve for each direction
            Curve longU = GetLongestCurve(U);
            Curve longV = GetLongestCurve(V);

            //get parameters
            List<double> tU = GetSortedParameters(U, longV);
            List<double> tV = GetSortedParameters(V, longU);

            //sort lists
            U = SortByList(U, tU);
            iU = SortByList(iU, tU);
            V = SortByList(V, tV);
            iV = SortByList(iV, tV);

            if (FU)
            {
                U.Reverse();
                iU.Reverse();
            }
            if (FV)
            {
                V.Reverse();
                iV.Reverse();
            }
            //output
            UO = U;
            UI = iU;
            VO = V;
            VI = iV;
        }

        #region PREVIEW OVERRIDES

        private List<Curve> _previewUCurves = new List<Curve>();
        private List<Curve> _previewVCurves = new List<Curve>();

        //Draw all wires and points in this method.
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            PreviewCurveGradient(args, _previewUCurves, Color.Red, Color.Cyan);
            PreviewCurveGradient(args, _previewVCurves, Color.Yellow, Color.Green);
        }

        private static void PreviewCurveGradient(IGH_PreviewArgs args, List<Curve> crvs, Color start, Color end)
        {
            for (int i = 0; i < crvs.Count; i++)
            {
                //calculate gradient
                double percent = i / (crvs.Count * 1.00);

                byte r = (byte)(start.R * (1 - percent) + end.R * percent);
                byte g = (byte)(start.G * (1 - percent) + end.G * percent);
                byte b = (byte)(start.B * (1 - percent) + end.B * percent);
                byte a = (byte)(start.A * (1 - percent) + end.A * percent);

                Color color = Color.FromArgb(a, r, g, b);

                //preview curve
                args.Display.DrawCurve(crvs[i], color);
            }
        }

        #endregion PREVIEW OVERRIDES

        #region METHODS

        private static Curve GetLongestCurve(List<Curve> curves)
        {
            Curve longC = curves[0];
            double length = longC.GetLength();
            for (int i = 1; i < curves.Count; i++)
            {
                Curve tempC = curves[i];
                if (tempC.GetLength() < length) { continue; }
                length = tempC.GetLength();
                longC = tempC;
            }
            return longC;
        }

        private static List<double> GetSortedParameters(List<Curve> curves, Curve sorter)
        {
            List<double> parameters = new List<double>();
            foreach (Curve curve in curves)
            {
                double param = ClosestParameter(curve, sorter);
                //Print(param.ToString());
                parameters.Add(param);
            }
            return parameters;
        }

        private static double ClosestParameter(Curve curveA, Curve curveB)
        {
            //try to get intersection
            // Calculate the intersection
            double intersection_tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double overlap_tolerance = 0.0;
            var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, intersection_tolerance, overlap_tolerance);
            if (events != null)
            {
                if (events.Count > 0)
                {
                    var ccx_event = events[0];
                    return ccx_event.ParameterB;
                }
            }

            //if no intersection try plane

            //if no plane try end points
            //format so it takes into account distance from end/start of measurement curve
            Point3d s = curveA.PointAtStart;
            Point3d e = curveA.PointAtEnd;
            double sT;
            curveB.ClosestPoint(s, out sT);
            Point3d sCP = curveB.PointAt(sT);
            double sDist = s.DistanceTo(sCP);

            double eT;
            curveB.ClosestPoint(e, out eT);
            Point3d eCP = curveB.PointAt(eT);
            double eDist = e.DistanceTo(eCP);

            //find out which one is closer
            Point3d pt = s;
            Point3d cp = sCP;
            double dist = sDist;
            if (eDist < sDist)
            {
                pt = e;
                cp = eCP;
                dist = eDist;
            }

            //found out if this point is closer to the end or the start
            double t = eT + dist;
            if (pt.DistanceTo(curveB.PointAtStart) < pt.DistanceTo(curveB.PointAtEnd))
            {
                t = sT - dist;
            }

            return t;
        }

        private static List<Curve> SortByList(List<Curve> list, List<double> sorter)
        {
            if (list.Count != sorter.Count) { return null; }
            SortedDictionary<double, Curve> dict = new SortedDictionary<double, Curve>();
            for (int i = 0; i < list.Count; i++)
            {
                if (dict.ContainsKey(sorter[i])) { continue; }
                dict.Add(sorter[i], list[i]);
            }

            return dict.Values.ToList(); ;
        }

        private static List<int> SortByList(List<int> list, List<double> sorter)
        {
            if (list.Count != sorter.Count) { return null; }
            SortedDictionary<double, int> dict = new SortedDictionary<double, int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (dict.ContainsKey(sorter[i])) { continue; }
                dict.Add(sorter[i], list[i]);
            }

            return dict.Values.ToList(); ;
        }

        #endregion METHODS
    }
}
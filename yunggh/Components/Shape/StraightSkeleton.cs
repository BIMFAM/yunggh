using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class StraightSkeleton : GH_Component
    {
        public StraightSkeleton()
          : base("Straight Skeleton", "Straight Skeleton",
              "Calculate Straight Skeleton",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve for erosion", GH_ParamAccess.item);
            pManager.AddNumberParameter("Double", "D", "Double for distance of each curve", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Skeleton", "S", "Curves for straight skeleton", GH_ParamAccess.list);
            pManager.AddCurveParameter("Frame", "F", "Curves for erossion frame", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            Double step = 0;
            if (!DA.GetData(0, ref crv) || !DA.GetData(1, ref step)) return;
            List<Curve> erossionCurves = getErossionCurves(crv, step);

            List<Polyline> frame;
            List<LineCurve> skeleton;
            getSkeleton(erossionCurves, out frame, out skeleton);

            DA.SetDataList(0, skeleton);  // output skeleton
            DA.SetDataList(1, frame);  // output frame
        }

        private static void getSkeleton(List<Curve> crvList, out List<Polyline> frame, out List<LineCurve> skeleton)
        {
            List<Polyline> pllList = new List<Polyline>();  // convert curves to polyline
            frame = new List<Polyline>();
            skeleton = new List<LineCurve>();
            foreach (Curve curCrv in crvList)
            {
                Polyline pll;
                curCrv.TryGetPolyline(out pll);
                pllList.Add(pll);
            }
            foreach (Polyline pll in pllList)
            {
                if (frame.Count == 0)
                {
                    frame.Add(pll);
                    continue;
                }
                Polyline prePll = frame[frame.Count - 1];

                /*
                foreach(Point3d pt in pll){
                  Point3d closestPt = Rhino.Collections.Point3dList.ClosestPointInList(prePll, pt);
                  Polyline newPll = new Polyline();
                  newPll.Add(closestPt);
                  newPll.Add(pt);
                  output.Add(newPll);
                }*/
                //if(prePll.Count > pll.Count + 1) continue;
                foreach (Point3d ppt in prePll)
                {
                    Point3d cpt = pll[0];
                    foreach (Point3d pt in pll)
                    {
                        if (ppt.DistanceTo(pt) < ppt.DistanceTo(cpt)) cpt = pt;
                    }

                    LineCurve newLine = new LineCurve(ppt, cpt);
                    int count = Rhino.Geometry.Intersect.Intersection.CurveCurve(prePll.ToNurbsCurve(), newLine, 0, 0).Count;
                    if (count > 1) continue;
                    skeleton.Add(newLine);
                }
                frame.Add(pll);
            }
        }

        private static List<Curve> getErossionCurves(Curve crv, double step)
        {
            List<Curve> output = new List<Curve>();//output list
            crv.TryGetPlane(out Plane p);   //get the offset plane
            Queue<Curve> q = new Queue<Curve>();    // BFS hold curves
            q.Enqueue(crv);
            double minArea = double.PositiveInfinity;   // set max to min area
            for (int i = 0; i < 10000; i++)  //recursive limited times in case of infinite loop
            {
                if (q.Count == 0) break;
                Curve curCrv = q.Dequeue();
                AreaMassProperties currArea = AreaMassProperties.Compute(curCrv);   // count center point and area
                if (currArea.Area < minArea) minArea = currArea.Area;   // if a curve is larger than previous one, break
                else break;

                if (output.Count != 0)   // check intersections between curves
                {
                    Rhino.Geometry.Intersect.CurveIntersections intersect =
                    Rhino.Geometry.Intersect.Intersection.CurveCurve(curCrv, output[output.Count - 1], 0.001, 0.001);
                    if (intersect.Count > 0) break; // if there are intersections, break
                }
                output.Add(curCrv); // add current curve to output
                //Point3d centerPoint = currArea.Centroid;    // offset directions, could it be better?
                //Curve[] nextCrvs = curCrv.Offset(centerPoint, new Vector3d(0, 0, 1), step, 0.001, CurveOffsetCornerStyle.Sharp);
                Curve[] nextCrvs = curCrv.Offset(p, -step, 0.01, CurveOffsetCornerStyle.Sharp); // offset by plane, this would be better!
                if (nextCrvs == null) break;
                foreach (Curve cv in nextCrvs)
                {
                    if (cv.IsClosed) q.Enqueue(cv);
                }
            }
            return output;
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Erosion;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("E0800B7B-1E8A-4B19-80FF-8AB57EE39D3D"); }
        }
    }
}
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class Erosion : GH_Component
    {
        public Erosion()
          : base("Erosion", "Erosion",
              "Calculate Erosion",
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
            pManager.AddCurveParameter("Curve", "C", "Curves for erossion", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            Double step = 0;
            if (!DA.GetData(0, ref crv) || !DA.GetData(1, ref step)) return;
            List<Curve> output = new List<Curve>();//output list

            Plane p = new Plane(new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(0, 1, 0));  // using for offset
            Queue<Curve> q = new Queue<Curve>();    // BFS hold curves
            q.Enqueue(crv);
            double minArea = double.PositiveInfinity;   // set max to min area
            for (int i = 0; i < 1000; i++)  //recursive limited times in case of infinite loop
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
                Curve[] nextCrvs = curCrv.Offset(p, step, 0.001, CurveOffsetCornerStyle.Sharp); // offset by plane, this would be better!
                if (nextCrvs == null) break;
                foreach (Curve cv in nextCrvs) {
                    if (cv.IsClosed) q.Enqueue(cv);
                }
                    
            }
            DA.SetDataList(0, output);  // output list
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
            get { return new Guid("29270538-1ADF-4972-B6EA-41A341E0B27D"); }
        }
    }
}
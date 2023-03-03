using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class PolarConvexity : GH_Component
    {
        public PolarConvexity()
          : base("Polar Convexity", "PolarConvexity",
              "Calculate Polar Convexity",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Point for center", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Curve for walls", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Int", "I", "Integer for steps", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("Vector", "V", "Vectors for lines", GH_ParamAccess.list);
            pManager.AddLineParameter("Line", "L", "Lines for polar convexity", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "Points for intersection", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d centerPoint = new Point3d();
            Curve curve = null;
            Int32 step = 0;

            if (!DA.GetData(1, ref curve) || !DA.GetData(2, ref step)) return;
            if (!DA.GetData(0, ref centerPoint)) centerPoint = AreaMassProperties.Compute(curve).Centroid;

            List<Point3d> points = new List<Point3d>(); //output points
            List<Line> lines = new List<Line>();  //output lines
            List<Vector3d> vectors = new List<Vector3d>();  //output vectors

            // generate each vector
            Point3d startPoint = curve.PointAtStart;
            Vector3d startVector = new Vector3d(startPoint.X - centerPoint.X, startPoint.Y - centerPoint.Y, startPoint.Z - centerPoint.Z);
            Vector3d axis = new Vector3d(0, 0, 1);
            for (int i = 0; i < step; i++)
            {
                vectors.Add(new Vector3d(startVector));
                startVector.Rotate(Math.PI / step, axis);
            }

            // points filter
            foreach (Vector3d vt in vectors)
            {
                Line curLine = new Line(centerPoint, vt, 1);
                List<Point3d> pointList = getPoint(curLine, curve);

                Point3d farPoint = new Point3d(int.MaxValue, int.MaxValue, int.MaxValue);
                Point3d firstPoint = farPoint;  //nearest point in one direction
                Point3d secondPoint = farPoint; //nearest point in the other direction

                foreach (Point3d p in pointList)
                {
                    if (p.X - centerPoint.X > 0)
                    {
                        if (firstPoint.DistanceTo(centerPoint) > p.DistanceTo(centerPoint)) firstPoint = p;
                    }
                    else
                    {
                        if (secondPoint.DistanceTo(centerPoint) > p.DistanceTo(centerPoint)) secondPoint = p;
                    }
                }
                if (firstPoint.CompareTo(farPoint) != 0) points.Add(firstPoint);
                if (secondPoint.CompareTo(farPoint) != 0) points.Add(secondPoint);
            }

            //generate lines
            foreach (Point3d pt in points)
            {
                lines.Add(new Line(centerPoint, pt));
            }

            //Points = points;
            //Vectors = vectors;
            //Lines = lines;
            DA.SetDataList(2, points);
            DA.SetDataList(0, vectors);
            DA.SetDataList(1, lines);
        }

        private List<Point3d> getPoint(Line line, Curve curve)
        {
            List<Point3d> xpoints = new List<Point3d>();
            List<object> xoverlap = new List<object>();

            Rhino.Geometry.Intersect.CurveIntersections xe =
            Rhino.Geometry.Intersect.Intersection.CurveLine(curve, line, 0, 0);

            foreach (Rhino.Geometry.Intersect.IntersectionEvent x in xe)
            {
                if (x.IsPoint)
                {
                    xpoints.Add(x.PointA); //intersection points as seen on first curve
                                           //xpoints.Add(x.PointB); //intersection points on other curves
                }
                else
                { //x is not a point but an overlap
                  //xoverlap.Add(new Line(x.PointA, x.PointA2));
                  //xoverlap.Add(new Line(x.PointB, x.PointB2));
                }
            }
            return xpoints;
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.PolarConvexity;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9E86165B-F210-46CF-8BFD-8C4CD973D5A9"); }
        }
    }
}
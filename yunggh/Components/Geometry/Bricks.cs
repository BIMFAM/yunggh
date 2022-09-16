using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh.Components.Geometry
{
    public class Bricks : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Bricks class.
        /// </summary>
        public Bricks()
          : base("Bricks", "BRCKS",
              "Populates a Curve (C) with Bricks",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to populate with bricks", GH_ParamAccess.item);
            pManager.AddNumberParameter("Brick Length", "L", "Brick Length", GH_ParamAccess.item);
            pManager.AddNumberParameter("Brick Width", "W", "Brick Width", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Bricks", "B", "Brick Polylines", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            Curve C = null;//C is a curve to lay bricks along
            double L = 0;//L is the length of each brick
            double W = 0;//W is the width of each brick
            if (!DA.GetData(0, ref C)) return;
            if (!DA.GetData(1, ref L)) return;
            if (!DA.GetData(2, ref W)) return;

            //original code from C# Grasshopper Component
            const double tolerance = 0.001; //constant tolerance for curve intersections

            double length = C.GetLength(); //get the curve length
            double totalBrickLength = 0; //to keep track of the total length of our bricks so far

            //offset curve based on thickness
            double widthHalf = W / 2.0;
            Curve offset0 = C.Offset(Plane.WorldXY, -widthHalf, tolerance, CurveOffsetCornerStyle.Smooth)[0];
            Curve offset1 = C.Offset(Plane.WorldXY, widthHalf, tolerance, CurveOffsetCornerStyle.Smooth)[0];

            List<object> bricks = new List<object>(); //output list to keep brick polylines
                                                      //bricks.Add(offset0);
                                                      //bricks.Add(offset1);

            double lastParam = 0;
            Point3d start = C.PointAtStart;
            while (totalBrickLength < length - L)
            {
                //Print("Brick");
                totalBrickLength += L; //increment brick distance with each

                //make a circle to get the intersection point for this brick
                Circle circle = new Circle(start, L);
                var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(C, circle.ToNurbsCurve(), tolerance, 0.0);

                double param = events.OrderBy(o => o.ParameterA).Reverse().ToList()[0].ParameterA;
                if (param < lastParam) { break; }
                //Print(param.ToString());
                Point3d end = C.PointAt(param); //update point

                //make brick out of polyline points
                Vector3d forward = end - start;
                Vector3d right = forward;
                right.Rotate(Math.PI / 2.0, Vector3d.ZAxis); //rotate to get perpendicular offset direction
                right.Unitize(); //make length 1

                //create brick polylne
                Point3d S0 = start + (right * widthHalf);
                Point3d S1 = start + (right * -widthHalf);
                Point3d E0 = end + (right * widthHalf);
                Point3d E1 = end + (right * -widthHalf);
                Polyline brick = new Polyline() { S0, S1, E1, E0, S0 };
                bricks.Add(brick.ToNurbsCurve());

                //update information, by calculating which point we should rotate the brick off of
                Vector3d endTangent = C.TangentAt(param); //get tangent vector at end
                Transform xform = Transform.Translation(endTangent);//make move out of vector
                end.Transform(xform); //move point
                                      //find out which end point is closer to the moved point
                Point3d measurePoint = E0; //set point to measure from
                Curve offset = offset0; //set curve to measure along
                if (end.DistanceTo(E0) > end.DistanceTo(E1)) { measurePoint = E1; offset = offset1; } //make sure we are on the interior/concave side of the curve, this will flip if an "S" curve

                //using the offset curves find the brick tangent vector, to create the start point for the next brick
                circle = new Circle(measurePoint, L); //make a circle based off measure Point
                                                      //bricks.Add(circle);
                events = Rhino.Geometry.Intersect.Intersection.CurveCurve(offset, circle.ToNurbsCurve(), tolerance, 0.0);
                param = events.OrderBy(o => o.ParameterA).Reverse().ToList()[0].ParameterA;
                end = offset.PointAt(param); //get end point along concave curve
                Vector3d normal = end - measurePoint; //get direction along curve between two points
                Plane perp = new Plane(measurePoint, normal); //make a plane using this direction
                                                              //bricks.Add(perp);
                var inter = Rhino.Geometry.Intersect.Intersection.CurvePlane(C, perp, tolerance); //get intersection between curve and plane
                param = inter.OrderBy(o => o.PointA.DistanceTo(measurePoint)).ToList()[0].ParameterA; //sort intersections by distance to measurepoint in case of duplicate intersections
                start = C.PointAt(param); //set new start point
                lastParam = param; //make sure we don't duplicate the last brick
            }

            //set output
            DA.SetDataList(0, bricks);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Brick;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DAAC7D62-F758-42A4-BA3E-17242A401F61"); }
        }
    }
}
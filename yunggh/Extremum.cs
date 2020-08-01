using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class Extremum : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Extremum()
          : base("Extremum", "EXR",
              "Finds the extremum minimum or maximum of a brep (points/curves).",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Base plane for spiral", GH_ParamAccess.item);
            pManager.AddVectorParameter("Direction", "D", "Vectors representing extremum directions", GH_ParamAccess.list);
            pManager.AddBooleanParameter("MinMax", "M", "True returns the maximum extremum, False returns the minimum extremum", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Extremum", "X", "Brep extremums", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            Brep brep = null;
            List<Vector3d> directions = new List<Vector3d>();
            bool minmax = true;
            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, directions)) return;
            if (!DA.GetData(2, ref minmax)) return;

            //warnings
            if (brep == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Brep is null.");
                return;
            }

            //main script
            List<Object> extremums = FindExtremums(brep, directions, minmax);

            DA.SetDataList(0, extremums);
        }

        /// <summary>
        /// Find the extremum of a brep.
        /// </summary>
        /// <param name="brep">Brep for extremum calculation</param>
        /// <param name="directions">Extremum normal vector</param>
        /// <param name="minmax">True returns the maximum, False returns the minimum</param>
        /// <returns>List of extremums, either points or curves.</returns>
        private List<Object> FindExtremums(Brep brep, List<Vector3d> directions, bool minmax)
        {
            double tolerance = DocumentTolerance();
            List<Object> extremums = new List<Object>();
            //Rhino.Geometry.GeometryBase
            foreach (Vector3d normal in directions)
            {
                Plane plane = new Plane(new Point3d(0, 0, 0), normal);

                //create bounding box
                Box worldBox;
                BoundingBox box = brep.GetBoundingBox(plane, out worldBox);

                //using the world box corners, we create top or bottom planes
                Point3d[] corners = worldBox.GetCorners();
                Plane intersection = new Plane(corners[0], corners[1], corners[2]); //bottom plane
                if (!minmax)
                    intersection = new Plane(corners[4], corners[5], corners[6]); //top plane

                Curve[] crvs;
                Point3d[] pts;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, intersection, 0, out crvs, out pts)) { continue; }

                foreach (Curve crv in crvs)
                    extremums.Add(crv);
                foreach (Point3d pt in pts)
                    extremums.Add(pt);

                if (crvs.Length > 0 || pts.Length > 0) continue;

                //if no curve intersections were found, we need to get all the edges of the brep and check for intersections.
                List<Point3d> Xpts = new List<Point3d>();
                foreach (Curve edg in brep.Edges)
                {
                    Rhino.Geometry.Intersect.CurveIntersections Xcrv = Rhino.Geometry.Intersect.Intersection.CurvePlane(edg, intersection, tolerance);
                    if (Xcrv == null) continue;

                    for (int i = 0; i < Xcrv.Count; i++)
                    {
                        Rhino.Geometry.Intersect.IntersectionEvent crvX = Xcrv[i];
                        Xpts.Add(crvX.PointA);
                    }
                }
                Point3d[] culledXpts = Point3d.CullDuplicates(Xpts, tolerance);
                foreach (Point3d pt in culledXpts)
                    extremums.Add(pt);
            }

            return extremums;
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to septenary),
        /// each of which can be combined with the GH_Exposure.obscure flag, which
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Extremum;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ac8caf98-7e00-465d-ab43-6b033e848a52"); }
        }
    }
}
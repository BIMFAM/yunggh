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
    public class OrientedBoundingBox : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public OrientedBoundingBox()
          : base("Oriented Bounding Box", "OBB",
              "Orients a bounding box for geometric fit",
              "yung gh", "Geometry")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep for bounding box", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Bounding Box", "B", "Oriented bounding box", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base Plane", "P", "Base plane for bounding box", GH_ParamAccess.item);
            pManager.AddVectorParameter("Normal", "N", "Bounding box normal (up) direction", GH_ParamAccess.item);
            pManager.AddVectorParameter("Forward", "F", "Bounding box forward direction", GH_ParamAccess.item);
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

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref brep)) return;

            //warnings
            if (brep == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Brep is null.");
                return;
            }

            //main function

            //get surface normal from largest surface of brep
            Point3d origin = new Point3d(0, 0, 0);
            Vector3d normal = new Vector3d(0, 0, 1);
            double largestArea = 0;
            bool NoPlanarSurfacesFound = true;
            foreach (Surface srf in brep.Surfaces)
            {
                if (!srf.IsPlanar()) continue;// we only want to use planar surfaces

                Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(srf);
                if (area.Area < largestArea) continue;

                largestArea = area.Area;
                origin = Rhino.Geometry.AreaMassProperties.Compute(srf).Centroid;

                double u; double v;
                srf.ClosestPoint(origin, out u, out v);
                normal = srf.NormalAt(u, v); //set the normal of the largest surface
                NoPlanarSurfacesFound = false;
            }
            //if no planar surface was found, use largest surface
            if (NoPlanarSurfacesFound)
            {
                foreach (Surface srf in brep.Surfaces)
                {
                    Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(srf);
                    if (area.Area <= largestArea) continue;

                    largestArea = area.Area;
                    origin = Rhino.Geometry.AreaMassProperties.Compute(srf).Centroid;

                    double u; double v;
                    srf.ClosestPoint(origin, out u, out v);
                    normal = srf.NormalAt(u, v); //set the normal of the largest surface
                    NoPlanarSurfacesFound = false;
                }
            }

            //get forward direction vector from longest line of brep
            Vector3d forward = new Vector3d(0, 0, 0);
            double longestLength = 0;
            bool NoLinearCurveFound = true;
            foreach (Curve crv in brep.Edges)
            {
                //we are only interested in linear curves
                if (!crv.IsLinear()) continue;

                double length = crv.GetLength();
                if (length < longestLength) continue;

                longestLength = length;
                forward = crv.PointAtEnd - crv.PointAtStart;
                NoLinearCurveFound = false;
            }

            //if no linear curve was found, use longest curve
            if (NoLinearCurveFound)
            {
                foreach (Curve crv in brep.Edges)
                {
                    double length = crv.GetLength();
                    if (length < longestLength) continue;
                    if (crv.PointAtEnd.DistanceTo(crv.PointAtStart) < DocumentTolerance()) continue;

                    longestLength = length;
                    forward = crv.PointAtEnd - crv.PointAtStart;
                    NoLinearCurveFound = false;
                }
            }

            //contruct orientation plane from normal and forward vector
            Plane plane = new Plane(origin, normal);
            forward.Transform(Rhino.Geometry.Transform.PlanarProjection(plane));
            forward.Unitize();
            double angle = Vector3d.VectorAngle(plane.YAxis, forward, plane);
            plane.Rotate(angle, normal);

            //create bounding box
            Box worldBox;
            BoundingBox box = brep.GetBoundingBox(plane, out worldBox);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, worldBox);
            DA.SetData(1, plane);
            DA.SetData(2, normal);
            DA.SetData(3, forward);
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
                return Resource.OrientedBoundingBox;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("696a7e35-b71a-4b25-ac5d-af54b7084ec8"); }
        }
    }
}
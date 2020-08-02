using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class SplitBrepPlane : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SplitBrepPlane()
          : base("Split Brep Plane", "BXP",
              "Split a Brep with Plane(s).",
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
            pManager.AddBrepParameter("Brep", "B", "Brep to split with Plane(s).", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane(s) for splitting.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Splits", "S", "Split Brep", GH_ParamAccess.list);
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
            List<Plane> planes = new List<Plane>();

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, planes)) return;

            //warnings
            if (brep == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Brep is null.");
                return;
            }

            //main function
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Brep> splits = SafeSplit(new List<Brep>() { brep }, planes, tolerance);

            //set the splits as the output
            DA.SetDataList(0, splits);
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="P"> List of planes to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        private List<Brep> SafeSplit(List<Brep> breps, List<Plane> P, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (P.Count == 0) return breps;

            Plane plane = P[0];
            P.RemoveAt(0);
            foreach (Brep brep in breps)
            {
                Curve[] intersections;
                Point3d[] pt;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, plane, tolerance, out intersections, out pt)) { splits.Add(brep); continue; }

                //if the intersection failed, we keep the original brep
                if (intersections.Length == 0) { splits.Add(brep); continue; }

                Brep[] test = brep.Split(intersections, tolerance); //it will always only be one intersection at a time

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (P.Count != 0)
            {
                splits = SafeSplit(splits, P, tolerance);
            }

            return splits;
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
                return Resource.SplitBrepPlane;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("da3c5e28-fc1f-41a2-9f59-a18216bc908f"); }
        }
    }
}
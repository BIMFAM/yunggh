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
    public class SplitKeepRemove : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SplitKeepRemove()
          : base("Split Keep Remove", "SKR",
              "Split brep with curves, using points to decide which splits to keep or remove.",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep to split", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curves", "C", "Curve(s) to split brep with", GH_ParamAccess.list);
            pManager.AddPointParameter("Keep/Remove Points", "P", "If these points are on a split brep, that brep will be kept/removed", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Keep/Remove", "X", "True keeps the points, False removes the points", GH_ParamAccess.item, true);
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
            List<Curve> curves = new List<Curve>();
            List<Point3d> points = new List<Point3d>();
            bool X = true; //true keeps splits touching points, false removes splits touching points

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, curves)) return;
            if (!DA.GetDataList(2, points)) return;
            if (!DA.GetData(3, ref X)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            //warnings
            if (brep == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Brep is null.");
                return;
            }

            //get document tolerance for splitting
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //split the brep using safe split
            List<Brep> splits = SafeSplit(new List<Brep>() { brep }, curves, tolerance);

            //find out which splits should be kept or removed
            List<int> keepIndices = BrepPointCheck(splits, points, tolerance);

            //output splits according to selection type
            List<Brep> keepSplits = new List<Brep>();
            for (int i = 0; i < splits.Count; i++)
            {
                //if we are in the keep mode, we keep splits that are touching
                if (keepIndices.Contains(i) && X == true)
                    keepSplits.Add(splits[i]);

                //if we are in the remove mode, we keep splits that aren't touching
                if (!keepIndices.Contains(i) && X == false)
                    keepSplits.Add(splits[i]);
            }

            // Finally assign the spiral to the output parameter.
            DA.SetDataList(0, keepSplits);
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="C"> List of curves to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        private List<Brep> SafeSplit(List<Brep> breps, List<Curve> C, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (C.Count == 0) return breps;

            Curve crv = C[0];
            C.RemoveAt(0);
            foreach (Brep brep in breps)
            {
                Brep[] test = brep.Split(new List<Curve>() { crv }, tolerance); //it will always only be one intersection at a time

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (C.Count != 0)
            {
                splits = SafeSplit(splits, C, tolerance);
            }

            return splits;
        }

        /// <summary>
        /// Checks which breps a list of points is touching
        /// </summary>
        /// <param name="breps">List of breps for testing point touch</param>
        /// <param name="pts">List of points to check if brep is touching</param>
        /// <param name="tolerance"> Tolerance for point check</param>
        /// <returns>The indices of the breps touched by points</returns>
        private List<int> BrepPointCheck(List<Brep> breps, List<Point3d> pts, double tolerance)
        {
            List<int> indices = new List<int>();

            foreach (Point3d pt in pts)
            {
                for (int i = 0; i < breps.Count; i++)
                {
                    Brep brep = breps[i];
                    Point3d cp = brep.ClosestPoint(pt);
                    double dist = pt.DistanceTo(cp);
                    if (dist <= tolerance)
                    {
                        indices.Add(i);
                    }
                }
            }
            return indices;
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
                return Resource.SplitKeepRemove;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b8dba31e-991e-4c63-8d1f-7d5e16418b9b"); }
        }
    }
}
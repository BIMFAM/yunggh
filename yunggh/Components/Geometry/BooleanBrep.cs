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
    public class BooleanBrep : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public BooleanBrep()
          : base("BooleanBrep", "BBR",
              "Creates a Brep from additive and subtractive Breps.",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Additive Breps", "B", "Breps to boolean union.", GH_ParamAccess.list);
            pManager.AddBrepParameter("Subtractive Breps", "B", "Breps to boolean difference.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Boolean Brep", "B", "Boolean Brep result.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            List<Brep> UnionBreps = new List<Brep>();
            List<Brep> DifferenceBreps = new List<Brep>();

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetDataList(0, UnionBreps)) return;
            if (!DA.GetDataList(1, DifferenceBreps)) return;

            // warnings
            if (UnionBreps.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Union Breps is empty");
                return;
            }

            // main
            Brep booleanBrep = GetBooleanBrep(UnionBreps, DifferenceBreps);

            // Finally assign the boolean brep to the output parameter.
            DA.SetData(0, booleanBrep);
        }

        private Brep GetBooleanBrep(List<Brep> unionBreps, List<Brep> differenceBreps = null)
        {
            // internal parameters
            double tolerance = 0.01;
            Brep outputBrep = null;
            YungGH yunggh = new YungGH();

            // Find union of all input union breps
            Brep[] unionBrepArray = Brep.CreateBooleanUnion(unionBreps, tolerance, true);
            if (unionBrepArray != null)
            {
                Brep unionResultBrep = yunggh.FindLargestBrepByVolume(unionBrepArray);
                outputBrep = unionResultBrep;

                // Difference all difference breps from this Brep
                if (differenceBreps != null)
                {
                    List<Brep> unionBrepList = new List<Brep>() { unionResultBrep };
                    Brep[] differenceBrepArray = Brep.CreateBooleanDifference(unionBrepList, differenceBreps, tolerance, true);
                    if (differenceBrepArray != null)
                    {
                        Brep differenceResultBrep = yunggh.FindLargestBrepByVolume(differenceBrepArray);
                        outputBrep = differenceResultBrep;
                    }
                }
            }

            return outputBrep;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("068794ed-0a68-4397-865b-85dfd5f0c0d7"); }
        }
    }
}

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
    public class Bake : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Bake()
         : base("Bake", "Bake",
            "Bake geometry",
            "yung gh", "Format")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddBooleanParameter("Bake", "B", "Boolean for bake operation.", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake.", GH_ParamAccess.list);
            pManager.AddTextParameter("Layer", "L", "Layer for each geometry", GH_ParamAccess.list);
            // If you want to change properties of certain parameters,
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddBooleanParameter("Baked", "B", "Boolean indicating successful bake operation.", GH_ParamAccess.item);
            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            bool run = false;
            List<GeometryBase> geometry = new List<GeometryBase>();
            List<string> layer = new List<string>();

            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetDataList(1, geometry)) return;
            if (!DA.GetDataList(2, layer)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (geometry.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Geometry not input");
                return;
            }
            if (layer.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer not input");
                return;
            }

            // We're set to create the spiral now. To keep the size of the SolveInstance() method small,
            // The actual functionality will be in a different method:
            if (run)
            {
                YungGH yunggh = new YungGH();
                yunggh.BakeGeometry(geometry, layer);
            }

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, true);
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
                return Resource.Bake;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd548"); }
        }
    }
}
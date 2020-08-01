using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

using System.IO;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class ReadCSV : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadCSV()
          : base("Read CSV", "Read CSV",
              "Read CSV",
              "yung gh", "Data")
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
            pManager.AddTextParameter("Filepath", "F", "Filepath for csv document", GH_ParamAccess.item);
            pManager.AddTextParameter("Delimiter", "D", "Char to split file with", GH_ParamAccess.item);
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
            pManager.AddBooleanParameter("Read", "R", "Boolean indicating if successfully read.", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "D", "CSV data formatted to tree", GH_ParamAccess.tree);
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
            string filepath = "";
            string delimiter = "";

            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref filepath)) return;
            if (!DA.GetData(1, ref delimiter)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (!File.Exists(filepath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Filepath does not exist");
                return;
            }
            if (delimiter.Length > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Delimiter must be single character");
                return;
            }

            // We're set to create the spiral now. To keep the size of the SolveInstance() method small,
            // The actual functionality will be in a different method:
            GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> data = ImportCSV(filepath, delimiter);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, true);
            DA.SetDataTree(1, data);
        }

        private GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ImportCSV(string filepath, string delimiter)
        {
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>
                treeArray = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();

            string[] text = System.IO.File.ReadAllLines(filepath);

            for (int i = 0; i < text.Length; i++)
            {
                string[] parts = text[i].Split(delimiter[0]); //split row
                for (int j = 0; j < parts.Length; j++)
                {
                    Grasshopper.Kernel.Data.GH_Path ghpath = new Grasshopper.Kernel.Data.GH_Path(i);
                    Grasshopper.Kernel.Types.GH_String cell = new Grasshopper.Kernel.Types.GH_String(parts[j]);
                    treeArray.Append(cell, ghpath);
                }
            }
            return treeArray;
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
                return Resource.ReadCSV;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd555"); }
        }
    }
}
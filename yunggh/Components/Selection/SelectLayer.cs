using System;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class SelectLayer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SelectLayer()
          : base("Select Layer", "Select Layer",
              "Select all objects by a layer (and child layer).",
              "yung gh", "Selection")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Layer", "L", "Layers to select geometry.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Prompt", "P", "Select layer geometry.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Guids", "ID", "Selected Guids", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            GH_Structure<GH_String> layers;
            bool run = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetDataTree<GH_String>(0, out layers)) return;
            if (!DA.GetData(1, ref run)) return;

            // main

            //make sure outputs aren't null
            if (guids == null) { guids = new GH_Structure<GH_Guid>(); }

            //Assign the selected guids to the output parameter.
            //DA.SetData(0, guids);

            //return when button isn't pressed
            if (!run && !pending) return;

            //return & set pending to true
            if (!pending)
            { pending = true; return; }

            // reset pending to false
            pending = false;
            guids = new GH_Structure<GH_Guid>();
            YungGH yunggh = new YungGH();

            //loop through all the full layer paths
            foreach (GH_Path path in layers.Paths)
            {
                GH_String[] gh_strings = layers[path].ToArray();
                List<GH_Guid> appendGuids = new List<GH_Guid>();
                for (int j = 0; j < gh_strings.Length; j++)
                {
                    string fullLayerPath = gh_strings[j].Value;
                    Rhino.DocObjects.Layer layer = yunggh.LayerByFullPath(fullLayerPath);
                    List<System.Guid> layerObjects = yunggh.SelectObjectsByLayer(Rhino.RhinoDoc.ActiveDoc, layer);

                    foreach (System.Guid guid in layerObjects)
                    {
                        appendGuids.Add(new GH_Guid(guid));
                    }
                }

                GH_Path newPath = new GH_Path(path);
                guids.AppendRange(appendGuids, newPath);
            }

            //Assign the selected guids to the output parameter.
            DA.SetDataTree(0, guids);
        }

        private bool pending = false;

        private GH_Structure<GH_Guid> guids;

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
                return Resource.SelectLayer;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd561"); }
        }
    }
}
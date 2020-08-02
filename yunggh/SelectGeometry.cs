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
    public class SelectGeometry : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SelectGeometry()
          : base("Select Geometry", "Select Geometry",
              "Prompt user to select geometry.",
              "yung gh", "Selection")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Guids", "ID", "Geometry Guids to select", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Prompt", "P", "Prompt user to select geometry.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Guids", "ID", "Spiral curve", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Selected", "S", "Selected Guids", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            GH_Structure<IGH_Goo> inputGuids;
            bool run = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetDataTree<IGH_Goo>(0, out inputGuids)) return;
            if (!DA.GetData(1, ref run)) return;

            // main

            //make sure outputs aren't null
            if (guids == null) { guids = new GH_Structure<GH_Guid>(); }
            if (selected == null) { selected = new GH_Structure<GH_Boolean>(); }

            //return when button isn't pressed
            if (!run && !pending) return;

            //return & set pending to true
            if (!pending)
            { pending = true; return; }

            // reset pending to false
            pending = false;
            guids = new GH_Structure<GH_Guid>();
            selected = new GH_Structure<GH_Boolean>();

            //loop through all the guids
            foreach (GH_Path path in inputGuids.Paths)
            {
                IGH_Goo[] gh_guids = inputGuids[path].ToArray();
                List<GH_Guid> appendGuids = new List<GH_Guid>();
                List<GH_Boolean> appendSelected = new List<GH_Boolean>();

                for (int j = 0; j < gh_guids.Length; j++)
                {
                    GH_Guid gh_guid = (GH_Guid)gh_guids[j];
                    //if (!gh_guids[j].CastTo<GH_Guid>(out gh_guid)) continue;
                    if (gh_guid == null) continue;

                    System.Guid guid = gh_guid.Value;
                    appendGuids.Add(gh_guid);
                    if (Rhino.RhinoDoc.ActiveDoc.Objects.Select(guid, true, true, true))
                    {
                        appendSelected.Add(new GH_Boolean(true));
                    }
                    else
                    {
                        appendSelected.Add(new GH_Boolean(false));
                    }
                }

                GH_Path newPath = new GH_Path(path);
                guids.AppendRange(appendGuids, newPath);
                selected.AppendRange(appendSelected, newPath);
            }

            //Assign the selected geometry guids to the output parameter.
            DA.SetDataTree(0, guids);
            DA.SetDataTree(1, selected);
        }

        private bool pending = false;

        private GH_Structure<GH_Boolean> selected;
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
                return Resource.SelectGeometry;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd560"); }
        }
    }
}
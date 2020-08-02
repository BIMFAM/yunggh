using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Collections.Specialized;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class ReadAttributes : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReadAttributes()
          : base("Read Attributes", "Read Attributes",
              "Read attribute information from rhino objects",
              "yung gh", "Data")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GUID", "ID", "Rhino GUID", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Rhino Object Name", GH_ParamAccess.item);
            pManager.AddTextParameter("Keys", "K", "List of Attribute Keys", GH_ParamAccess.list);
            pManager.AddTextParameter("Values", "V", "List of Attribute Values", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            System.Guid guid = System.Guid.Empty;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref guid)) return;

            //warnings
            if (guid == System.Guid.Empty)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "GUID is empty.");
                return;
            }

            //main
            Rhino.DocObjects.RhinoObject obj = Rhino.RhinoDoc.ActiveDoc.Objects.Find(guid);
            if (obj == null) return;

            Dictionary<string, string> attributes = ReadObjectAttributes(obj);

            //Assign the object attributes to the output parameters.
            DA.SetData(0, obj.Name);
            DA.SetDataList(1, attributes.Keys);
            DA.SetDataList(2, attributes.Values);
        }

        public Dictionary<string, string> ReadObjectAttributes(Rhino.DocObjects.RhinoObject obj)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            Rhino.DocObjects.ObjectAttributes objattributes = new Rhino.DocObjects.ObjectAttributes();
            objattributes = obj.Attributes;

            NameValueCollection keyValues = objattributes.GetUserStrings();
            foreach (string key in keyValues)
            {
                attributes.Add(key, keyValues[key]);
            }
            return attributes;
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
                return Resource.ReadAttributes;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd547"); }
        }
    }
}
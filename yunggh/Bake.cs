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
                BakeGeometry(geometry, layer);
            }

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, true);
        }

        private void BakeGeometry(List<GeometryBase> geometries, List<string> layers)
        {
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            int layer = -1;
            string name = "";

            for (int i = 0; i < geometries.Count; i++)
            {
                //find layer
                if (i < layers.Count)
                {
                    layer = GetLayerIndex(doc, layers[i]);
                }

                //create attributes
                Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = layer;
                attributes.Name = name;
                //attributes.ColorSource = ObjectColorSource.ColorFromObject;
                //attributes.ObjectColor = Color.Black;

                //bake geometry
                doc.Objects.Add(geometries[i], attributes);
            }
        }

        private int GetLayerIndex(Rhino.RhinoDoc doc, string layer)
        {
            string layer_name = layer.Trim();

            //does layer name contain data?
            if (string.IsNullOrEmpty(layer_name))
            {
                Rhino.RhinoApp.WriteLine("Layer name cannot be blank.");
                return -1;
            }

            // Is the layer name valid?
            if (!Rhino.DocObjects.Layer.IsValidName(layer_name))
            {
                Rhino.RhinoApp.WriteLine(layer_name + " is not a valid layer name.");
                return -1;
            }

            // Does a layer with the same name already exist?            
            Rhino.DocObjects.Layer layer_object = doc.Layers.FindName(layer_name, -1);
            int layer_index = -1;
            if (layer_object != null)
            {
                layer_index = layer_object.Index;
            }
            //int layer_index = doc.Layers.Find(layer_name, true);
            if (layer_index >= 0)
            {
                Rhino.RhinoApp.WriteLine("A layer with the name {0} already exists.", layer_name);
                return layer_index;
            }

            // Add a new layer to the document
            layer_index = doc.Layers.Add(layer_name, System.Drawing.Color.Black);
            if (layer_index < 0)
            {
                Rhino.RhinoApp.WriteLine("Unable to add {0} layer.", layer_name);
                return -1;
            }
            Rhino.RhinoApp.WriteLine("{0} layer added.", layer_name);
            return layer_index;
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
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd548"); }
        }
    }
}
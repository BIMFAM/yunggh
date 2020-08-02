using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

using System.Drawing;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class UpdateLayer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UpdateLayer()
          : base("Create Layer", "Create Layer",
              "Creates a layer.",
              "yung gh", "Document")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Layer Path", "N", "Full Layer Path", GH_ParamAccess.item, "");

            pManager.AddColourParameter("Layer Color", "C", "Layer Color", GH_ParamAccess.item, Color.Black);
            pManager.AddTextParameter("Material", "M", "Layer Material", GH_ParamAccess.item, "");

            pManager.AddBooleanParameter("Locked", "L", "Is Locked", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("On/Off", "O", "Layer Visibility", GH_ParamAccess.item, true);

            pManager.AddTextParameter("Plot Line Type", "PT", "Plot Line Type", GH_ParamAccess.item, "");
            pManager.AddColourParameter("Plot Line Color", "PC", "Plot Color", GH_ParamAccess.item, Color.Black);
            pManager.AddNumberParameter("Plot Line Width", "PW", "Plot Line Width", GH_ParamAccess.item, 1.0);

            pManager.AddBooleanParameter("Delete Layer", "D", "Delete Layer", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Layer Path", "L", "Updated/Created Full Layer Path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            string fullLayerPath = "";
            Color color = Color.Empty;
            string material = "";
            bool locked = false;
            bool onOff = true;
            string plotLineType = "";
            Color plotLineColor = Color.Empty;
            double plotLineWidth = 1.0;
            bool delete = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref fullLayerPath)) return;
            if (!DA.GetData(1, ref color)) return;
            if (!DA.GetData(2, ref material)) return;
            if (!DA.GetData(3, ref locked)) return;
            if (!DA.GetData(4, ref onOff)) return;
            if (!DA.GetData(5, ref plotLineType)) return;
            if (!DA.GetData(6, ref plotLineColor)) return;
            if (!DA.GetData(7, ref plotLineWidth)) return;
            if (!DA.GetData(8, ref delete)) return;

            // Warnings
            if (String.IsNullOrEmpty(fullLayerPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "layer path is null or empty");
                return;
            }

            // Main
            Rhino.DocObjects.Layer layer = CreateModify(Rhino.RhinoDoc.ActiveDoc, fullLayerPath, color, locked,
                plotLineType, material, onOff, plotLineColor, plotLineWidth, delete);

            string returnPath = "";
            if (layer != null)
                returnPath = layer.FullPath;

            // Assign the created or updated full layer path to the output parameter.
            DA.SetData(0, returnPath);
        }

        private const string layerDelimiter = "::";

        public Rhino.DocObjects.Layer LayerByFullPath(string layerPath)
        {
            Rhino.DocObjects.Layer layer = null;

            foreach (Rhino.DocObjects.Layer tempLayer in RhinoDoc.ActiveDoc.Layers)
            {
                if (tempLayer.FullPath != layerPath) continue;

                layer = tempLayer;
                break;
            }

            return layer;
        }

        public void DeleteObjectsOnLayer(RhinoDoc doc, Rhino.DocObjects.Layer layer)
        {
            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer);
            foreach (Rhino.DocObjects.RhinoObject rhobj in rhobjs)
            {
                doc.Objects.Delete(rhobj, true, true);
            }
        }

        private Rhino.DocObjects.Layer CreateModify(RhinoDoc doc, string layerPath, System.Drawing.Color color,
          bool locked, string lineTypeName,
          string materialName, bool onOff,
          System.Drawing.Color printColor, double printWidth, bool delete)
        {
            //get layer if already existing
            Rhino.DocObjects.Layer layer = LayerByFullPath(layerPath);

            //if we want to delete the layer
            if (delete)
            {
                if (layer == null) return null;

                //make sure it's not the current layer
                if (doc.Layers.CurrentLayer.Index == layer.Index)
                    doc.Layers.SetCurrentLayerIndex(0, true); //TODO: if they are deleting this layer index it will throw an error
                DeleteObjectsOnLayer(doc, layer);
                doc.Layers.Delete(layer.Index, true);
                return null;
            }

            //if the target layer does not exist, create it
            if (layer == null)
            {
                //if it is a single layer path
                if (!layerPath.Contains(layerDelimiter))
                {
                    layer = new Rhino.DocObjects.Layer { Id = Guid.NewGuid(), Name = layerPath, Index = doc.Layers.Count };
                    int index = doc.Layers.Add(layer);
                }
                //if the path has tree depth
                else
                {
                    string[] fullPath = layerPath.Split(new string[] { layerDelimiter }, StringSplitOptions.None);
                    string parent = "";
                    foreach (string nextPath in fullPath)
                    {
                        layer = new Rhino.DocObjects.Layer { Id = Guid.NewGuid(), Name = nextPath, Index = doc.Layers.Count };
                        if (parent != "")
                        {
                            string parentName = parent.Substring(2, parent.Length - 2); //remove the delimiter from the front
                            Rhino.DocObjects.Layer parentLayer = LayerByFullPath(parentName);
                            layer.ParentLayerId = parentLayer.Id;
                        }

                        int index = doc.Layers.Add(layer);
                        parent += layerDelimiter + nextPath;
                    }
                }

                //get layer after it's been added
                layer = LayerByFullPath(layerPath);
            }

            if (layer == null) return layer;

            //layer color
            if (color != null) layer.Color = color;

            //layer is locked
            layer.IsLocked = locked;

            //layer visibility
            layer.IsVisible = onOff;

            //linetype
            Rhino.DocObjects.Linetype lineType = doc.Linetypes.FindName(lineTypeName);
            if (lineType != null) layer.LinetypeIndex = lineType.LinetypeIndex;

            //layer material
            layer.RenderMaterialIndex = doc.Materials.Find(materialName, true);

            //print width
            layer.PlotWeight = printWidth;

            //print color
            layer.PlotColor = printColor;

            return layer;
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
                return Resource.UpdateLayer;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd569"); }
        }
    }
}
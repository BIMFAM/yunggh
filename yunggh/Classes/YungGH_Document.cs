using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    internal partial class YungGH
    {
        /// <summary>
        /// Rhino full layer path delimiter
        /// </summary>
        private const string layerDelimiter = "::";

        /// <summary>
        /// Get a Layer by the full path name.
        /// </summary>
        /// <param name="layerPath">Full layer path</param>
        /// <returns>Rhino Layer</returns>
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

        /// <summary>
        /// Delete objects on a layer
        /// </summary>
        /// <param name="doc">Rhino Document</param>
        /// <param name="layer">Full layer path</param>
        public void DeleteObjectsOnLayer(RhinoDoc doc, Rhino.DocObjects.Layer layer)
        {
            //TODO: return deleted Guids
            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer);
            foreach (Rhino.DocObjects.RhinoObject rhobj in rhobjs)
            {
                doc.Objects.Delete(rhobj, true, true);
            }
        }

        /// <summary>
        /// Modify a Layer in the Rhino document. If no layer exists, a layer is created. The layer name uses the full layer path.
        /// </summary>
        /// <param name="doc">Rhino Document</param>
        /// <param name="layerPath">Full layer path</param>
        /// <param name="color">Layer color</param>
        /// <param name="locked">Layer locked status</param>
        /// <param name="lineTypeName">Layer line type</param>
        /// <param name="materialName">Layer material name (if not existing, is set to default material)</param>
        /// <param name="onOff">Layer visibility status</param>
        /// <param name="printColor">Layer plot color</param>
        /// <param name="printWidth">Layer plot width</param>
        /// <param name="delete">If true, the layer is deleted</param>
        /// <returns>Rhino Layer</returns>
        public Rhino.DocObjects.Layer CreateModify(RhinoDoc doc, string layerPath, System.Drawing.Color color,
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
        /// Get the layer index from the layer name
        /// </summary>
        /// <param name="doc">Rhino Document</param>
        /// <param name="layer">Layer name</param>
        /// <returns>Layer index</returns>
        public int GetLayerIndex(Rhino.RhinoDoc doc, string layer)
        {
            //TODO: merge with Get Full Layer Path
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
        /// Resets all the Button components
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Arguements</param>
        public void ResetButtonComponents(object sender, GH_SolutionEventArgs e)
        {
            e.Document.SolutionEnd -= ResetButtonComponents;

            //GH_Document doc = this.OnPingDocument();
            /*/
            foreach (IGH_DocumentObject obj in doc.Objects) //Search all components in the Canvas
            {
                //Print(obj.Name);
                if (obj.Name != "Button") continue;

                //obj.ExpireSolution(true); //recompute this component, this is a way to restart upstream
                //obj.ExpirePreview(true);
            }
            //*/
        }

        /// <summary>
        /// Get all active Guids from the Rhino document
        /// </summary>
        /// <returns>A list of Guids of active objects in the rhino document</returns>
        public List<System.Guid> GetGuids()
        {
            List<System.Guid> guids = new List<System.Guid>();
            foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects)
            {
                guids.Add(obj.Id);
            }
            return guids;
        }
    }
}
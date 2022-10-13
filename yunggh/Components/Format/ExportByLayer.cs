using System;
using System.Collections.Generic;
using System.Diagnostics;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class ExportByLayer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ExportByLayer()
          : base("ExportByLayer", "ExportByLayer",
              "Mesh geometry and join meshes by layer before exporting",
              "yung gh", "Format")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Export", "E", "Run Export", GH_ParamAccess.item);
            pManager.AddTextParameter("Filepaths", "F", "Export Filepath", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Keep Mesh", "K", "Keep Meshes after combination.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Get input data from Grasshopper
            bool run = false;
            string filepath = string.Empty;
            bool keep = false;
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetData(1, ref filepath)) return;
            if (!DA.GetData(2, ref keep)) return;
            if (!run) { return; }

            //get all objects
            var guids = YungGH.GetGuids(); Debug.WriteLine("Guids.Count: " + guids.Count);

            //organize objects by layer
            var doc = Rhino.RhinoDoc.ActiveDoc;
            Dictionary<int, List<GeometryBase>> objects = new Dictionary<int, List<GeometryBase>>();
            foreach (var guid in guids)
            {
                var rhobj = doc.Objects.FindId(guid);
                if (rhobj == null) { continue; }
                int layerIndex = rhobj.Attributes.LayerIndex;
                if (!objects.ContainsKey(layerIndex))
                {
                    objects.Add(layerIndex, new List<GeometryBase>());
                }
                objects[layerIndex].Add(rhobj.Geometry);
            }

            //combine objects into Meshes
            Dictionary<string, GeometryBase> joinedMeshes = new Dictionary<string, GeometryBase>();
            foreach (KeyValuePair<int, List<GeometryBase>> kvp in objects)
            {
                //get layer name
                var layerIndex = kvp.Key;
                string layerName = RhinoDoc.ActiveDoc.Layers.FindIndex(layerIndex).FullPath;
                var rhobjs = kvp.Value;
                Mesh mesh = new Mesh();
                foreach (var rhobj in rhobjs)
                {
                    if (rhobj is Mesh)
                    {
                        Mesh m = rhobj as Mesh;
                        mesh.Append(m);
                        continue;
                    }
                    if (rhobj is Brep)
                    {
                        Brep brep = rhobj as Brep;
                        var mesh_params = MeshingParameters.Default; //TODO: verify params (simplify)
                        var meshes = Mesh.CreateFromBrep(brep, mesh_params);
                        foreach (var m in meshes) { mesh.Append(m); }
                    }
                    if (rhobj is Surface)
                    {
                        Surface srf = rhobj as Surface;
                        var mesh_params = MeshingParameters.Default;
                        var m = Mesh.CreateFromSurface(srf, mesh_params);
                        mesh.Append(m);
                    }
                }

                joinedMeshes.Add(layerName, mesh);
            }

            //bake objects as mesh
            List<GeometryBase> geoBase = joinedMeshes.Values.ToList();
            List<string> layers = joinedMeshes.Keys.ToList();
            List<string> names = new List<string>();
            for (int i = 0; i < layers.Count; i++)
            {
                string layer = layers[i];
                if (!layer.Contains("::")) { names.Add(layer); continue; }//if this is the topmost layer we just continue

                List<string> parts = layer.Split(new char[] { ':', ':' }).ToList();
                string name = parts[parts.Count - 1];
                names.Add(name);
                parts.RemoveAt(parts.Count - 1);
                parts.RemoveAll(s => s == "");
                layer = string.Join("::", parts);
                layers[i] = layer;
            }

            guids = YungGH.BakeGeometry(geoBase, layers, names); //TODO: Make sure objects have material of layer name assigned

            //export objects
            YungGH.Select(Rhino.RhinoDoc.ActiveDoc, guids);
            if (Path.GetExtension(filepath) != ".fbx")
            {
                filepath = Path.ChangeExtension(filepath, ".fbx");
            }
            YungGH.ExportModel(filepath);
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
                return Resource.ExportByLayer;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("85DADB87-9082-4FFE-A991-4F1E9D722809"); }
        }
    }
}
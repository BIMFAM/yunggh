// Copyright (c) 2022 archgame
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

﻿using System;
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
using System.Drawing;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class ExportByMaterial : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ExportByMaterial()
          : base("ExportByMaterial", "ExportByMaterial",
              "Mesh geometry and join meshes by material before exporting",
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

            //Grasshopper Button Issue Fix
            if (!run && !pending) return; //return when button isn't pressed
            if (!pending) { pending = true; return; }//return & set pending to true
            pending = false; // reset pending to false

            //get all objects
            var guids = YungGH.GetGuids(); Debug.WriteLine("Guids.Count: " + guids.Count);

            //organize objects by layer
            var doc = Rhino.RhinoDoc.ActiveDoc;
            Dictionary<int, List<GeometryBase>> objects = new Dictionary<int, List<GeometryBase>>();
            foreach (var guid in guids)
            {
                var rhobj = doc.Objects.FindId(guid);
                if (rhobj == null) { continue; }
                int materialIndex = rhobj.Attributes.MaterialIndex;
                if (materialIndex == -1) //get layer material if there isn't one on the object
                {
                    int layerIndex = rhobj.Attributes.LayerIndex;
                    var layer = RhinoDoc.ActiveDoc.Layers.FindIndex(layerIndex);
                    materialIndex = layer.RenderMaterialIndex;
                }
                if (!objects.ContainsKey(materialIndex))
                {
                    objects.Add(materialIndex, new List<GeometryBase>());
                }
                objects[materialIndex].Add(rhobj.Geometry);
            }

            //combine objects into Meshes
            Dictionary<string, GeometryBase> joinedMeshes = new Dictionary<string, GeometryBase>();
            foreach (KeyValuePair<int, List<GeometryBase>> kvp in objects)
            {
                //get layer name
                var materialIndex = kvp.Key;
                var mat = RhinoDoc.ActiveDoc.Materials.FindIndex(materialIndex);

                string materialName = "NoMaterialSelected";
                if (materialIndex != -1)
                {
                    materialName = RhinoDoc.ActiveDoc.Materials.FindIndex(materialIndex).Name;
                }
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
                        var mesh_params = MeshingParameters.FastRenderMesh;
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

                mesh.RebuildNormals();
                //mesh.Vertices.CombineIdentical(true, true); //is this necessary?
                //mesh.Normals.ComputeNormals(); //is this necessary?
                //mesh.UnifyNormals(); //is this necessary?
                //mesh.Compact();
                mesh.Faces.CullDegenerateFaces();

                //join if already existing
                if (joinedMeshes.ContainsKey(materialName))
                {
                    var geo = joinedMeshes[materialName];
                    Mesh m = geo as Mesh;
                    mesh.Append(m);
                    joinedMeshes[materialName] = mesh;
                    continue;
                }

                joinedMeshes.Add(materialName, mesh);
            }

            //make basic layer to hold all the meshes
            string fullLayerPath = "Materials";
            Color color = Color.Black;
            string material = "";
            bool locked = false;
            bool onOff = true;
            string plotLineType = "";
            Color plotLineColor = Color.Black;
            double plotLineWidth = 1.0;
            bool delete = false;
            YungGH.CreateModify(Rhino.RhinoDoc.ActiveDoc, fullLayerPath, color, locked,
                plotLineType, material, onOff, plotLineColor, plotLineWidth, delete);

            //bake objects as mesh by material
            List<GeometryBase> geoBase = joinedMeshes.Values.ToList();
            List<string> layers = new List<string>();
            List<string> materials = joinedMeshes.Keys.ToList();
            for (int i = 0; i < materials.Count; i++)
            {
                layers.Add(fullLayerPath);
            }

            guids = YungGH.BakeGeometry(geoBase, layers, materials, materials); //TODO: Make sure objects have material of layer name assigned

            //export objects
            YungGH.Select(Rhino.RhinoDoc.ActiveDoc, guids);
            if (Path.GetExtension(filepath) != ".fbx")
            {
                filepath = Path.ChangeExtension(filepath, ".fbx");
            }
            YungGH.ExportModel(filepath);

            //delete baked elements if keep is false
            if (keep) { return; }
            doc.Objects.Delete(guids, true);
        }

        private bool pending = false;

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
                return Resource.ExportByMaterial;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("022A3B3E-5E10-4BA5-ACF5-75E054CEB158"); }
        }
    }
}
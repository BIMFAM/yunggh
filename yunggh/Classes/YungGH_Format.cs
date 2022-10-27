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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Threading;
using System.Diagnostics;

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
        /// Bake geometry from Grasshopper into the Rhino Document
        /// </summary>
        /// <param name="geometries">List of geometries to "Bake"</param>
        /// <param name="layers">Layers to "Bake" geometry to</param>
        public static List<Guid> BakeGeometry(List<GeometryBase> geometries, List<string> layers, List<string> names = null, List<string> materials = null)
        {
            List<Guid> guids = new List<Guid>();
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            int layerIndex = -1;
            string name = "";

            for (int i = 0; i < geometries.Count; i++)
            {
                //find layer
                if (i < layers.Count)
                {
                    Rhino.DocObjects.Layer layer = LayerByFullPath(layers[i]);
                    if (layer == null) continue; //TODO: create layer if it isn't existing

                    layerIndex = layer.Index;
                }

                //set name if available
                if (names != null) { if (i < names.Count) { name = names[i]; } }
                int matIndex = -1;
                if (materials != null)
                {
                    if (i < materials.Count)
                    {
                        matIndex = RhinoDoc.ActiveDoc.Materials.Find(materials[i], true);
                    }
                }
                //create attributes
                Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = layerIndex;
                attributes.Name = name;
                attributes.MaterialIndex = matIndex;
                if (matIndex != -1) { attributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject; }
                //attributes.ColorSource = ObjectColorSource.ColorFromObject;
                //attributes.ObjectColor = Color.Black;

                //bake geometry
                Guid guid = doc.Objects.Add(geometries[i], attributes);
                guids.Add(guid);
            }
            return guids;
        }

        /// <summary>
        /// Import a file into the Rhino Document
        /// </summary>
        /// <param name="filepath">import filepath</param>
        /// <returns>List of Guids from imported model.</returns>
        public List<System.Guid> ImportModel(string filepath)
        {
            List<System.Guid> importedGuids = new List<System.Guid>();

            //file exist guard statement
            if (!File.Exists(filepath)) return importedGuids;

            //rhino okay file type guard statement
            if (!SupportedImportFileTypes.Contains(Path.GetExtension(filepath))) return importedGuids;

            //get existing objects
            List<System.Guid> existingGuids = GetGuids();

            string import = string.Format("_-Import \"{0}\" _Enter", filepath);
            Rhino.RhinoApp.RunScript(import, false);

            importedGuids = GetGuids();

            //remove guids existing before the import
            importedGuids.RemoveAll(x => existingGuids.Contains(x));

            return importedGuids;
        }

        /// <summary>
        /// Export rhino geometry. Geometry should already be selected.
        /// </summary>
        /// <param name="filepath"></param>
        public static void ExportModel(string filepath)
        {
            string scriptExport = string.Format("_-Export \"{0}\" _Enter", filepath);
            RhinoApp.RunScript(scriptExport, false);
        }

        public static void ExportModel(string filepath, Point3d origin)
        {
            string scriptExport = string.Format("_-ExportWithOrigin {0} \"{1}\" _Enter", origin.ToString(), filepath);
            RhinoApp.RunScript(scriptExport, false);
        }

        /// <summary>
        /// A list of supported import file extensions for Rhino.
        /// </summary>
        public List<string> SupportedImportFileTypes = new List<string>(){
      ".3dm",".3dmbak",".rws",".3mf",".3ds",".amf",".ai",
      ".dwg",".dxf",".x",".e57",".dst",".exp",".eps",".off",
      ".gf",".gft",".gts",".igs",".iges",".lwo",".dgn",".fbx",
      ".scn",".obj",".pdf",".ply",".asc",".csv",".xyz",".cgo_ascii",
      ".cgo_asci",".pts",".txt",".raw",".m",".svg",".skp",".slc",
      ".sldprt",".sldasm",".stp",".step",".stl",".vda",".wrl",
      ".vrml",".vi",".gdf",".zpr"
      };

        /// <summary>
        /// A list of supported export file extensions for Rhino.
        /// </summary>
        public List<string> SupportedExportFileTypes = new List<string>(){
      ".3dm",".3dmbak",".rws",".3mf",".3ds",".amf",".ai",
      ".dwg",".dxf",".x",
      ".gf",".gft",".gts",".igs",".iges",".lwo",".dgn",".fbx",
      ".obj",".pdf",".ply",".csv",
      ".txt",".raw",".m",".svg",".skp",".slc",
      ".stp",".step",".stl",".vda",".wrl",
      ".vrml",".vi",".gdf",".zpr",
      ".dae",".cd",".emf",".pm",".kmz",".udo",".x_t",".rib",".wmf",".x3dv",".xaml",".xgl"
      };
    }
}
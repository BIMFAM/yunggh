using System;
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
        public void BakeGeometry(List<GeometryBase> geometries, List<string> layers)
        {
            //TODO: return Guids of baked geometry
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

                //create attributes
                Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = layerIndex;
                attributes.Name = name;
                //attributes.ColorSource = ObjectColorSource.ColorFromObject;
                //attributes.ObjectColor = Color.Black;

                //bake geometry
                doc.Objects.Add(geometries[i], attributes);
            }
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
        public void ExportModel(string filepath)
        {
            string scriptExport = string.Format("_-Export \"{0}\" _Enter", filepath);
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
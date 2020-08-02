using System;
using System.Collections.Generic;

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
    public class Import : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Import()
          : base("Import", "Import",
              "Import geometry",
              "yung gh", "Format")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Import", "I", "Use button to import files into Rhino document", GH_ParamAccess.item);
            pManager.AddTextParameter("Import Files", "F", "File(s) for importing.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("GUID", "ID", "Imported geometry GUID list", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            bool run = false;
            GH_Structure<GH_String> filepaths;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetDataTree(1, out filepaths)) return;

            // main
            if (guids == null)
            {
                guids = new GH_Structure<GH_Guid>();
            }

            //return when button isn't pressed
            if (!run && !pending) return;

            //return & set pending to true
            if (!pending)
            { pending = true; return; }

            // reset pending to false
            pending = false;

            //If we are running, establish a clean data tree
            guids = new GH_Structure<GH_Guid>();

            //loop through all the filepaths
            foreach (GH_Path path in filepaths.Paths)
            {
                GH_String[] gh_strings = filepaths[path].ToArray();

                foreach (GH_String gh_string in gh_strings)
                {
                    string filepath = gh_string.Value;

                    List<System.Guid> newGuids = ImportModel(filepath);
                    List<GH_Guid> appendGuids = new List<GH_Guid>();
                    foreach (System.Guid guid in newGuids)
                    {
                        appendGuids.Add(new GH_Guid(guid));
                    }

                    GH_Path newPath = new GH_Path(path);

                    guids.AppendRange(appendGuids, newPath);
                }
            }

            //if a button was used, it freezes, so we need to reset it
            //GrasshopperDocument.SolutionEnd += ResetButtonComponents; //this isn't reseting the canvas
            //GrasshopperDocument.SolutionEndEventHandler

            //Assign the guid data tree to the output parameter.
            DA.SetDataTree(0, guids);
        }

        private bool pending = false;

        private List<System.Guid> ImportModel(string filepath)
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

        public void ResetButtonComponents(object sender, GH_SolutionEventArgs e)
        {
            e.Document.SolutionEnd -= ResetButtonComponents;

            GH_Document doc = this.OnPingDocument();

            foreach (IGH_DocumentObject obj in doc.Objects) //Search all components in the Canvas
            {
                //Print(obj.Name);
                if (obj.Name != "Button") continue;

                //obj.ExpireSolution(true); //recompute this component, this is a way to restart upstream
                //obj.ExpirePreview(true);
            }
        }

        public List<System.Guid> GetGuids()
        {
            List<System.Guid> guids = new List<System.Guid>();
            foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects)
            {
                guids.Add(obj.Id);
            }
            return guids;
        }

        public List<string> SupportedImportFileTypes = new List<string>(){
      ".3dm",".3dmbak",".rws",".3mf",".3ds",".amf",".ai",
      ".dwg",".dxf",".x",".e57",".dst",".exp",".eps",".off",
      ".gf",".gft",".gts",".igs",".iges",".lwo",".dgn",".fbx",
      ".scn",".obj",".pdf",".ply",".asc",".csv",".xyz",".cgo_ascii",
      ".cgo_asci",".pts",".txt",".raw",".m",".svg",".skp",".slc",
      ".sldprt",".sldasm",".stp",".step",".stl",".vda",".wrl",
      ".vrml",".vi",".gdf",".zpr"
      };

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
                return Resource.Import;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd552"); }
        }
    }
}
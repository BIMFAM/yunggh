using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

using System.Drawing;

namespace yunggh
{
    public class DocFonts : GH_Component
    {
        public DocFonts()
          : base("Document Fonts", "DocFonts",
              "List all available document fonts.",
              "yung gh", "Document")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Font Names", "F", "Available Document Fonts", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Assign the created or updated full layer path to the output parameter.
            DA.SetDataList(0, Rhino.DocObjects.Font.AvailableFontFaceNames());
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7894C164-A3DC-40B3-9871-3A8178BF0AF4"); }
        }
    }
}
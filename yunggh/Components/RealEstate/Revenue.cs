using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class Revenue : GH_Component
    {
        public Revenue()
          : base("Revenue", "Revenue",
              "Calculate Revenue",
              "yung gh", "Real Estate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Revenue;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("EED9F65C-3D58-4810-B119-8FFF308A0B2D"); }
        }
    }
}
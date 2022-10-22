using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class ProForma2 : GH_Component
    {
        public ProForma2()
          : base("Pro Forma 2", "ProForma2",
              "Create a pro forma",
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
                return Resource.ProForma2;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0FBC1E4E-CF3B-463D-A8C7-3A1F16218B22"); }
        }
    }
}
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class ProForma4 : GH_Component
    {
        public ProForma4()
          : base("Pro Forma 4", "ProForma4",
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
                return Resource.ProForma4;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("35E59C78-8CD8-41F4-B8CF-8932E8B9EAAD"); }
        }
    }
}
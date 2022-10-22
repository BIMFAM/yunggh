using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class ProForma3 : GH_Component
    {
        public ProForma3()
          : base("Pro Forma 3", "ProForma3",
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
                return Resource.ProForma3;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8992B494-EDFD-4BDB-9AD8-3D755F7C91BB"); }
        }
    }
}
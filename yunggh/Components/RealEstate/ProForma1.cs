using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class ProForma1 : GH_Component
    {
        public ProForma1()
          : base("Pro Forma 1", "ProForma1",
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
                return Resource.ProForma1;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("13AEAF99-9975-42FB-99A0-0E850298276D"); }
        }
    }
}
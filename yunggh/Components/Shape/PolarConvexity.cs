using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class PolarConvexity : GH_Component
    {
        public PolarConvexity()
          : base("Polar Convexity", "PolarConvexity",
              "Calculate Polar Convexity",
              "yung gh", "Shape")
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
                return Resource.PolarConvexity;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9E86165B-F210-46CF-8BFD-8C4CD973D5A9"); }
        }
    }
}
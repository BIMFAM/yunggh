using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class Erosion : GH_Component
    {
        public Erosion()
          : base("Erosion", "Erosion",
              "Calculate Erosion",
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
                return Resource.Erosion;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("29270538-1ADF-4972-B6EA-41A341E0B27D"); }
        }
    }
}
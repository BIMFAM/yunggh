using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class MedialAxis : GH_Component
    {
        public MedialAxis()
          : base("Medial Axis", "MedialAxis",
              "Calculate Medial Axis",
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
                return Resource.MedialAxis;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0871EAD0-544C-4E0E-98A1-DCB5C559DF11"); }
        }
    }
}
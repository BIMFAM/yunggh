using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Geometry;

namespace yunggh
{
    public class MinimumBoundingRectangle : GH_Component
    {
        public MinimumBoundingRectangle()
          : base("Minimum Bounding Rectangle", "Bounding Rectangle",
              "Create the bounding rectangle",
              "yung gh", "Shape")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry base for bounding rectangle", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Rectangle", "R", "Minimum bounding rectangle", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GeometryBase geo = null;
            Rectangle3d output;
            if (!DA.GetData(0, ref geo)) return;

            Plane p = new Plane(0, 0, 1, 0);
            Point3d[] pts = geo.GetBoundingBox(p).GetCorners();
            output = new Rectangle3d(p, pts[0], pts[2]);

            DA.SetData(0, output);  // output rectangle
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.MedialAxis; // this should be replaced
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5FFF830D-C2D0-4B77-994D-8348FBD9CECE"); }
        }
    }
}
using Grasshopper.Kernel;
using Rhino.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yunggh;

namespace yunggh
{
    public class ToArcs : GH_Component
    {
        public ToArcs()
          : base("ToArcs", "ToArcs",
              "Convert Curve to Arcs and Lines",
              "yung gh", "Geometry")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to break into arcs.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Length", "MIN", "Minimum Length", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum Length", "MAX", "Maximum Length", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Arcs", "A", "Arc Segments.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get inputs
            Curve curve = null;
            double minimumLength = 0;
            double maximumLength = 0;
            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref minimumLength)) return;
            if (!DA.GetData(2, ref maximumLength)) return;
            if (curve == null) return;

            // Calculate Arcs
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double angleTolerance = Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceDegrees;
            var resultPolyCurve = curve.ToArcsAndLines(tolerance, angleTolerance * Math.PI / 180.0, minimumLength, maximumLength);
            if (resultPolyCurve == null) return;

            // Get Segments
            Curve[] segments = resultPolyCurve.Explode();
            var arcsList = new List<Curve>();
            foreach (Curve segment in segments)
            {
                if (segment is ArcCurve arcSegment)
                {
                    arcsList.Add(arcSegment);
                }
                else
                {
                    arcsList.Add(segment);
                }
            }

            DA.SetDataList(0, arcsList);
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("66F978EF-14B6-4198-8754-78F4A27E7623"); } }
    }
}
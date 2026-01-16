using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yunggh.Components.Panelization
{
    public class TrianglePanelization : PanelizationBase
    {
        #region UI

        public TrianglePanelization()
          : base("Triangle Panelization", "TRIPNL", "Panelize Surface with triangle panelization method.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("E98B3AA9-D083-45C5-A6AF-CDF3C3E7B407"); } }

        #endregion UI

        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            var allOutput = new List<List<List<Brep>>>();

            for (int r = 0; r < quads.Count; r++)
            {
                var rowOutput = new List<List<Brep>>();
                var row = quads[r];
                for (int j = 0; j < row.Count; j++)
                {
                    var panels = new List<Brep>();
                    var quad = row[j];
                    //TODO: create inheritance class
                    //test if quad is a triangle
                    if (quad[0] == Point3d.Unset || quad[1] == Point3d.Unset || quad[2] == Point3d.Unset || quad[3] == Point3d.Unset)
                    {
                        continue; //continue because quad is triangle
                    }

                    //create triangles
                    var trPnl1 = NurbsSurface.CreateFromCorners(quad[0], quad[1], quad[2]);
                    var trPnl2 = NurbsSurface.CreateFromCorners(quad[0], quad[2], quad[3]);
                    panels.Add(trPnl1.ToBrep());
                    panels.Add(trPnl2.ToBrep());
                    rowOutput.Add(panels);
                }
                allOutput.Add(rowOutput);
            }

            return allOutput;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return ListListListToTree(panels);
        }
    }
}
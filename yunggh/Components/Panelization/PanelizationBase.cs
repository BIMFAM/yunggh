using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace yunggh.Components.Panelization
{
    public abstract class PanelizationBase : GH_Component
    {
        private const string Category = "yung gh";
        private const string SubCategory = "Panelization";

        public PanelizationBase(string name, string nickname, string description)
          : base(name, nickname, description, Category, SubCategory)
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("EEA8B608-19AF-47FA-8E65-EC2E98BB9853"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Surface", "S", "Panelization Surface (can be double curved)", GH_ParamAccess.item);
            pManager.AddCurveParameter("U Curves", "U", "'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("V Curves", "V", "'V' Curves", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Flip U Curves", "FU", "Flip 'U' Curves", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip V Curves", "FV", "Flip 'V' Curves", GH_ParamAccess.item, false);

            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Panels", "P", "Panels", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //inputs
            var brep = new Brep();
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var uFlip = false;
            var vFlip = false;
            //object param1 = null;
            //object param2 = null;
            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetDataList(1, uCrvs)) return;
            if (!DA.GetDataList(2, vCrvs)) return;
            DA.GetData(3, ref uFlip);
            DA.GetData(4, ref vFlip);
            //DA.GetData(5, ref param1);
            //DA.GetData(6, ref param2);

            //main
            var uCrvsSorted = new List<Curve>();
            var vCrvsSorted = new List<Curve>();
            var uIndicesSorted = new List<int>();
            var vIndicesSorted = new List<int>();
            SortCurvesBySurface.Sort(uCrvs, vCrvs, uFlip, vFlip
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //get quad corners
            var quads = SurfaceQuadPoints.GetQuadCorners(uCrvsSorted, vCrvsSorted);

            //create panels
            var panels = GetPanels(quads, brep, DA);

            //output
            SetOuput(panels, DA);
        }

        public abstract List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA);

        public abstract void SetOuput(List<List<List<Brep>>> panels, IGH_DataAccess DA);
    }
}
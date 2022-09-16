using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace yunggh.Components.Geometry
{
    public class PlanarQuadMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PlanarQuadMesh class.
        /// </summary>
        public PlanarQuadMesh()
          : base("PQ Mesh", "PQMESH",
              "Create Planar Quads from Surface and Rails",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Curve to populate with bricks", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Rails", "R1", "Isoparametric Rails", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Rails", "R2", "Isoparametric Rails", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Planar Quads", "P", "Planar Quad Panels", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            GH_Structure<GH_Surface> surfacesData = new GH_Structure<GH_Surface>();
            GH_Structure<GH_Curve> rails1Data = new GH_Structure<GH_Curve>();
            GH_Structure<GH_Curve> rails2Data = new GH_Structure<GH_Curve>();
            if (!DA.GetDataTree(0, out surfacesData)) return;
            if (!DA.GetDataTree(1, out rails1Data)) return;
            if (!DA.GetDataTree(2, out rails2Data)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (surfacesData.DataCount == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "S (Surface) has no data"); return; }
            if (rails1Data.DataCount == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "R1 (Rails) has no data"); return; }
            if (rails2Data.DataCount == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "R2 (Rails) has no data"); return; }

            //main method
            var panelData = new GH_Structure<GH_Surface>();

            for (int i = 0; i < surfacesData.PathCount; i++)
            {
                //make sure we have a rail path at this point
                if (rails1Data.PathCount < i || rails2Data.PathCount < i) { break; }

                //get datas
                var rails1 = rails1Data.get_Branch(i).Cast<GH_Curve>().ToList(); ;
                var rails2 = rails2Data.get_Branch(i).Cast<GH_Curve>().ToList(); ;
                var path = surfacesData.Paths[i];
                var surfaces = surfacesData.get_Branch(i).Cast<GH_Surface>().ToList();

                //skip this path if any of the lists are missing data
                if (rails1.Count == 0 || rails2.Count == 0 || surfaces.Count == 0) { continue; }

                //run panelization routine
                var panels = YungGH.PQMeshSurface(surfaces[0], rails1, rails2);

                //add panels to output
                panelData.AppendRange(panels, path);
            }

            //set output
            DA.SetDataTree(0, panelData);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.PQMesh;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C496A463-3D66-4E17-8EBF-38468F4D7A91"); }
        }
    }
}
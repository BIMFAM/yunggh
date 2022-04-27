using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
namespace yunggh.Components.Mesh
{
    public class MeshVertex : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshVertex class.
        /// </summary>
        public MeshVertex()
          : base("MeshVertex", "MVRX",
              "Find vertices of Mesh",
              "yung gh", "Mesh")
        {
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Out", "Out", "Mesh", GH_ParamAccess.item);
            pManager.AddPointParameter("Faces", "F", "Mesh", GH_ParamAccess.item);
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3C0916D7-4852-42CD-ADFF-E35CB007065B"); }
        }
    }
}
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class Developability : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Developability()
          : base("Developability", "SRF DEV",
              "Test Surface Developability Type.",
              "yung gh", "Geometry")
        {
            CustomAttributes custom = new CustomAttributes(this);
            this.m_attributes = custom;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface for developability test.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Surface Type Text", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Integer", "I", "Surface Type Integer", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            Surface surface = null;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref surface)) return;

            // warnings
            if (surface == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Surface is null");
                return;
            }

            // main
            int type = 0;
            string text = TestSurfaceDevelopability(surface, out type);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, text);
            DA.SetData(1, type);
        }

        public string TestSurfaceDevelopability(Surface surface, out int type)
        {
            //https://discourse.mcneel.com/t/verifying-developable-surfaces/73594
            //https://discourse.mcneel.com/t/ruling-line-from-edge-curves-twist-check/73952

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //planar
            if (surface.IsPlanar(tolerance)) { type = 0; return "planar"; }

            //cylinder
            if (surface.IsCylinder(tolerance)) { type = 1; return "cylinder"; }

            //conic
            if (surface.IsCone(tolerance)) { type = 2; return "conic"; }

            //spheric
            if (surface.IsSphere(tolerance) || surface.IsTorus(tolerance)) { type = 4; return "double curved"; }

            //ruled surface

            Rhino.Geometry.Unroller unroll = null;
            Rhino.Geometry.Surface srf = surface;
            if (srf != null)
                unroll = new Rhino.Geometry.Unroller(srf);

            Rhino.Geometry.Brep brep = surface.ToBrep();

            if (unroll == null)
            {
                type = 4;
                return "double curved";
            }
            else
            {
                type = 3;
                return "ruled surface";
            }
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to septenary),
        /// each of which can be combined with the GH_Exposure.obscure flag, which
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Developabilitly;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f136ecde-559f-457f-9c91-2f3d6defed33"); }
        }
    }
}
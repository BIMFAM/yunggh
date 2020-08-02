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
    public class SimpleMeshCube : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SimpleMeshCube()
          : base("Simple Mesh Cube", "Mesh Cube",
              "Create a basic mesh cube",
              "yung gh", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point A", "A", "Point A", GH_ParamAccess.item);
            pManager.AddPointParameter("Point B", "B", "Point B", GH_ParamAccess.item);
            pManager.AddPointParameter("Point C", "C", "Point C", GH_ParamAccess.item);
            pManager.AddPointParameter("Point D", "D", "Point D", GH_ParamAccess.item);

            pManager.AddPointParameter("Point E", "E", "Point E", GH_ParamAccess.item);
            pManager.AddPointParameter("Point F", "F", "Point F", GH_ParamAccess.item);
            pManager.AddPointParameter("Point G", "G", "Point G", GH_ParamAccess.item);
            pManager.AddPointParameter("Point H", "H", "Point H", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Simple Mesh Cube", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            Point3d PTA = Point3d.Origin;
            Point3d PTB = Point3d.Origin;
            Point3d PTC = Point3d.Origin;
            Point3d PTD = Point3d.Origin;

            Point3d PTE = Point3d.Origin;
            Point3d PTF = Point3d.Origin;
            Point3d PTG = Point3d.Origin;
            Point3d PTH = Point3d.Origin;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref PTA)) return;
            if (!DA.GetData(1, ref PTB)) return;
            if (!DA.GetData(2, ref PTC)) return;
            if (!DA.GetData(3, ref PTD)) return;

            if (!DA.GetData(4, ref PTE)) return;
            if (!DA.GetData(5, ref PTF)) return;
            if (!DA.GetData(6, ref PTG)) return;
            if (!DA.GetData(7, ref PTH)) return;

            //warnings
            if (PTA == null || PTB == null || PTC == null || PTD == null || PTE == null || PTF == null || PTG == null || PTH == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Point is null.");
                return;
            }

            //Create mesh
            Mesh cube = MeshCube(PTA, PTB, PTC, PTD, PTE, PTF, PTG, PTH);

            //Assign the mesh to the output parameter.
            DA.SetData(0, cube);
        }

        public Mesh MeshCube(Point3d A, Point3d B, Point3d C, Point3d D, Point3d E, Point3d F, Point3d G, Point3d H)
        {
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            mesh.Vertices.Add(A); //0
            mesh.Vertices.Add(B); //1
            mesh.Vertices.Add(C); //2
            mesh.Vertices.Add(D); //3
            mesh.Vertices.Add(E); //4
            mesh.Vertices.Add(F); //5
            mesh.Vertices.Add(G); //6
            mesh.Vertices.Add(H); //7

            mesh.Faces.AddFace(7, 6, 5, 4);
            mesh.Faces.AddFace(1, 0, 4, 5);
            mesh.Faces.AddFace(2, 1, 5, 6);
            mesh.Faces.AddFace(3, 2, 6, 7);
            mesh.Faces.AddFace(0, 3, 7, 4);

            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
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
                return Resource.SimpleMeshCube;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd563"); }
        }
    }
}
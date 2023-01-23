// Copyright (c) 2022 archgame
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class Bake : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Bake()
         : base("Bake", "Bake",
            "Bake geometry duh2",
            "yung gh", "Format")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddBooleanParameter("Bake", "B", "Boolean for bake operation.", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Layer", "L", "Layer for each geometry", GH_ParamAccess.tree);
            // If you want to change properties of certain parameters,
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Baked", "B", "Boolean indicating successful bake operation.", GH_ParamAccess.item);
            pManager.AddGenericParameter("GUID", "ID", "Baked Geometry", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            // We'll start by declaring variables and assigning them starting values.
            bool run = false;
            Grasshopper.Kernel.Data.GH_Structure<IGH_GeometricGoo> geometries = new Grasshopper.Kernel.Data.GH_Structure<IGH_GeometricGoo>();
            Grasshopper.Kernel.Data.GH_Structure<GH_String> layers = new Grasshopper.Kernel.Data.GH_Structure<GH_String>();

            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetDataTree(2, out layers)) return;
            if (!DA.GetDataTree(1, out geometries)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (geometries.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geometry not input");
                return;
            }
            if (layers.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Layer not input");
                return;
            }

            // run each tree branch, including a safe layer in case the layer tree is less than the geometry branch
            if (run)
            {
                var guids = new DataTree<Guid>();
                List<string> lastLayer = new List<string> { };
                for (int i = 0; i < geometries.PathCount; i++)
                {
                    var path = geometries.Paths[i];

                    //get geometry
                    var geoList = geometries.get_Branch(path);
                    List<GeometryBase> geoBase = new List<GeometryBase>();
                    foreach (var geo in geoList)
                    {
                        if(geo == null) { continue; }
                        string test = geo.ToString();
                        Debug.WriteLine(test);
                        if (geo == null || test == "Singular Box") { continue; }
                        var g = GH_Convert.ToGeometryBase(geo);
                        if (g == null) { continue; }
                        geoBase.Add(g);
                    }

                    //get new layers if the list is matching the geometry branch count
                    if (layers.PathCount > i)
                    {
                        lastLayer = new List<string>();
                        var layerPath = layers.Paths[i];
                        if (layers.PathExists(path))
                        {
                            layerPath = path;
                        }
                        var layer = layers.get_Branch(layerPath);

                        foreach (var l in layer)
                        {
                            string name;
                            GH_Convert.ToString(l, out name, GH_Conversion.Primary);
                            lastLayer.Add(name);
                        }
                    }

                    var baked = YungGH.BakeGeometry(geoBase, lastLayer);
                    guids.AddRange(baked, path);
                }

                //output baked IDs
                DA.SetData(0, true);
                DA.SetDataTree(1, guids);
                return;
            }

            DA.SetData(0, false);
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
                return Resource.Bake;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd548"); }
        }
    }
}
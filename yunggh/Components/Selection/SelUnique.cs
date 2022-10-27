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

﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class SelUnique : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SelUnique()
          : base("SelUnique", "SelUnique",
              "Select unique objects.",
              "yung gh", "Selection")
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
            pManager.AddBooleanParameter("Prompt", "P", "Boolean to prompt user for input.", GH_ParamAccess.item);

            // If you want to change properties of certain parameters,
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddGenericParameter("Geometry", "G", "All Duplicates", GH_ParamAccess.list);
            //pManager.AddGeometryParameter("Geometry", "G", "All Duplicates", GH_ParamAccess.item);
            pManager.AddTextParameter("GUID", "ID", "GUIDs", GH_ParamAccess.list);
            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
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

            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref run)) return;

            // We're set to create the spiral now. To keep the size of the SolveInstance() method small,
            // The actual functionality will be in a different method:
            List<GeometryBase> geometry = new List<GeometryBase>();
            List<string> guids = new List<string>();
            IEnumerable<Rhino.DocObjects.RhinoObject> selected = GetSelUnique(run);
            if (selected != null)
            {
                foreach (Rhino.DocObjects.RhinoObject ro in selected)
                {
                    geometry.Add(ro.Geometry);
                    guids.Add(ro.Id.ToString());
                }
            }
            DA.SetDataList(0, geometry);
            DA.SetDataList(1, guids);
        }

        private IEnumerable<Rhino.DocObjects.RhinoObject> GetSelUnique(bool select)
        {
            if (!select && !pending) //return when button isn't pressed
            {
                return output;
            }
            if (!pending) //return & set pending to true
            {
                pending = true;
                return output;
            }

            if (pending) //pending
            {
                Rhino.RhinoApp.RunScript("_SelDupAll", false);
                IEnumerable<Rhino.DocObjects.RhinoObject> objects = Rhino.RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                //test which objects weren't selected during the SelDupAll
                var filter = new Rhino.DocObjects.ObjectEnumeratorSettings
                {
                    NormalObjects = true,
                    LockedObjects = false,
                    HiddenObjects = false,
                    ActiveObjects = true,
                    ReferenceObjects = true
                };

                List<Rhino.DocObjects.RhinoObject> uniqueObjects = new List<Rhino.DocObjects.RhinoObject>();
                Rhino.DocObjects.RhinoObject[] rh_objects = Rhino.RhinoDoc.ActiveDoc.Objects.FindByFilter(filter);
                foreach (Rhino.DocObjects.RhinoObject rh_obj in rh_objects)
                {
                    if (rh_obj.IsSelected(false) != 0 || !rh_obj.IsSelectable()) continue;

                    uniqueObjects.Add(rh_obj);
                }

                //unselect and select unique objects
                Rhino.RhinoDoc.ActiveDoc.Objects.UnselectAll();
                foreach (Rhino.DocObjects.RhinoObject rh_obj in uniqueObjects) { rh_obj.Select(true); }

                output = uniqueObjects;
            }

            pending = false;
            return output;
        }

        public IEnumerable<Rhino.DocObjects.RhinoObject> output;
        private bool pending = false;

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
                return Resource.SelUnique;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1c0053fa-49d1-4d6b-acad-294a2cb05638"); }
        }
    }
}
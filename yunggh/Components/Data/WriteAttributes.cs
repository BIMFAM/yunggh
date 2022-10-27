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
    public class WriteAttributes : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public WriteAttributes()
          : base("Write Attributes", "Write Attributes",
              "Write attribute information to rhino objects",
              "yung gh", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "GUID/Geometry", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Object Name", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Keys", "K", "List of Attribute Keys", GH_ParamAccess.list, new List<string>());
            pManager.AddTextParameter("Values", "V", "List of Attribute Values", GH_ParamAccess.list, new List<string>());
            pManager.AddBooleanParameter("Clean", "C", "Clears Attributes if true.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddGenericParameter("Attributes", "A", "Object Attributes", GH_ParamAccess.item);

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
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            object obj = null;
            string name = "";
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            bool clean = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref obj)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "0."); return; }
            if (!DA.GetData(1, ref name)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "1."); return; }
            if (!DA.GetDataList<string>(2, keys)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "2."); return; }
            if (!DA.GetDataList<string>(3, values)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "3."); return; }
            if (!DA.GetData(4, ref clean)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "4."); return; }

            //warning
            if (obj == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Object is null.");
                return;
            }

            // main
            YungGH yunggh = new YungGH();
            Rhino.DocObjects.ObjectAttributes objattributes = yunggh.WriteObjectAttributes(obj, name, keys, values, clean);

            //Assign the object attributes to the output parameter.
            DA.SetData(0, objattributes);
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
                return Resource.WriteAttributes;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cd3366d0-7b6e-41ae-a501-b59798ce54ca"); }
        }
    }
}
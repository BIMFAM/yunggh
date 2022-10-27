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
using System.IO;
using System.Threading;
using System.Diagnostics;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class WriteGoogleSheet : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public WriteGoogleSheet()
          : base("Write Google Sheet", "Write Google Sheet",
              "Writes data to a google sheet.",
              "yung gh", "Data")
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
            pManager.AddTextParameter("Auth", "A", "Authentication location", GH_ParamAccess.item);
            pManager.AddTextParameter("Spreadsheet ID", "S", "Google Spreadsheet ID", GH_ParamAccess.item);
            pManager.AddTextParameter("Tab", "T", "Tab Name", GH_ParamAccess.item);
            pManager.AddTextParameter("Data", "D", "List of data to write to spreadsheet", GH_ParamAccess.tree);

            // If you want to change properties of certain parameters,
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Write", "W", "Boolean indicating if successfully read.", GH_ParamAccess.item);
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
            string authentication = "";
            string spreadsheet = "";
            string tab = "";
            GH_Structure<Grasshopper.Kernel.Types.GH_String> data;
            // Then we need to access the input parameters individually.
            // When data cannot be extracted from a parameter, we should abort this method.
            if (!DA.GetData(0, ref authentication)) return;
            if (!DA.GetData(1, ref spreadsheet)) return;
            if (!DA.GetData(2, ref tab)) return;
            if (!DA.GetDataTree(3, out data)) return;

            // We should now validate the data and warn the user if invalid data is supplied.
            if (!File.Exists(authentication))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Authenitcation Failed");
                return;
            }
            if (spreadsheet == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Spreadsheet Parameter Empty");
                return;
            }
            if (tab == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tab Parameter Empty");
                return;
            }
            if (data.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Data Parameter Empty");
                return;
            }

            // main
            YungGH yunggh = new YungGH();
            bool written = yunggh.WriteGoogleSpreadsheet(authentication, spreadsheet, tab, data);

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, written);
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
                return Resource.WriteGooglesheet;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd568"); }
        }
    }
}
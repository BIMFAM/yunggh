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

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class Export : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Export()
          : base("Export", "Export",
              "Export geometry by file format",
              "yung gh", "Format")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Export", "E", "Run Export", GH_ParamAccess.item);
            pManager.AddGenericParameter("Objects", "O", "Objects to Export", GH_ParamAccess.tree);
            pManager.AddTextParameter("Filepaths", "F", "Export Filepath", GH_ParamAccess.tree);
            pManager.AddPointParameter("Origin", "O", "Exports File with Origin.", GH_ParamAccess.item, Point3d.Origin);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Exported Files", "E", "Exported Filepaths", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Get input data from Grasshopper
            bool run = false;
            GH_Structure<IGH_Goo> objects;
            GH_Structure<GH_String> inputFilePaths;
            Point3d origin = Point3d.Origin;
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetDataTree<IGH_Goo>(1, out objects)) return;
            if (!DA.GetDataTree<GH_String>(2, out inputFilePaths)) return;
            if (!DA.GetData(3, ref origin)) return;

            // main

            //return when button isn't pressed
            if (!run && !pending) return;

            //return & set pending to true
            if (!pending)
            { pending = true; return; }

            // reset pending to false
            pending = false;

            //establish a clean data tree
            filePaths = new GH_Structure<GH_String>();
            YungGH yunggh = new YungGH();

            //loop through all the input filepath paths
            foreach (GH_Path path in inputFilePaths.Paths)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.UnselectAll();
                List<System.Guid> guids = new List<System.Guid>();

                //select all objects
                IGH_Goo[] gh_guids = objects[path].ToArray();
                for (int j = 0; j < gh_guids.Length; j++)
                {
                    GH_Guid gh_guid = (GH_Guid)gh_guids[j];
                    if (gh_guid == null) continue;

                    System.Guid guid = gh_guid.Value;
                    guids.Add(guid);
                }

                YungGH.Select(Rhino.RhinoDoc.ActiveDoc, guids);

                //export objects
                GH_String[] filepaths = inputFilePaths[path].ToArray();
                for (int j = 0; j < filepaths.Length; j++)
                {
                    string filepath = filepaths[j].Value;

                    //if the file type is not supported, we skip it
                    if (!yunggh.SupportedExportFileTypes.Contains(Path.GetExtension(filepath))) continue;

                    //export
                    YungGH.ExportModel(filepath, origin);

                    GH_Path newPath = new GH_Path(path);
                    filePaths.AppendRange(new List<GH_String>() { new GH_String(filepath) }, newPath);
                }

                Rhino.RhinoDoc.ActiveDoc.Objects.UnselectAll();
            }

            // Assign the export filepaths to the output parameter.
            DA.SetDataTree(0, filePaths);
        }

        private bool pending = false;

        private GH_Structure<GH_String> filePaths;

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
                return Resource.Export;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd551"); }
        }
    }
}
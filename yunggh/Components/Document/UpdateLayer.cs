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
using Rhino;
using Rhino.Geometry;

using System.Drawing;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class UpdateLayer : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UpdateLayer()
          : base("Create Layer", "Create Layer",
              "Creates a layer.",
              "yung gh", "Document")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Layer Path", "N", "Full Layer Path", GH_ParamAccess.item, "");

            pManager.AddColourParameter("Layer Color", "C", "Layer Color", GH_ParamAccess.item, Color.Black);
            pManager.AddTextParameter("Material", "M", "Layer Material", GH_ParamAccess.item, "");

            pManager.AddBooleanParameter("Locked", "L", "Is Locked", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("On/Off", "O", "Layer Visibility", GH_ParamAccess.item, true);

            pManager.AddTextParameter("Plot Line Type", "PT", "Plot Line Type", GH_ParamAccess.item, "");
            pManager.AddColourParameter("Plot Line Color", "PC", "Plot Color", GH_ParamAccess.item, Color.Black);
            pManager.AddNumberParameter("Plot Line Width", "PW", "Plot Line Width", GH_ParamAccess.item, 1.0);

            pManager.AddBooleanParameter("Delete Layer", "D", "Delete Layer", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Layer Path", "L", "Updated/Created Full Layer Path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            string fullLayerPath = "";
            Color color = Color.Empty;
            string material = "";
            bool locked = false;
            bool onOff = true;
            string plotLineType = "";
            Color plotLineColor = Color.Empty;
            double plotLineWidth = 1.0;
            bool delete = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref fullLayerPath)) return;
            if (!DA.GetData(1, ref color)) return;
            if (!DA.GetData(2, ref material)) return;
            if (!DA.GetData(3, ref locked)) return;
            if (!DA.GetData(4, ref onOff)) return;
            if (!DA.GetData(5, ref plotLineType)) return;
            if (!DA.GetData(6, ref plotLineColor)) return;
            if (!DA.GetData(7, ref plotLineWidth)) return;
            if (!DA.GetData(8, ref delete)) return;

            // Warnings
            if (String.IsNullOrEmpty(fullLayerPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "layer path is null or empty");
                return;
            }

            // Main
            Rhino.DocObjects.Layer layer = YungGH.CreateModify(Rhino.RhinoDoc.ActiveDoc, fullLayerPath, color, locked,
                plotLineType, material, onOff, plotLineColor, plotLineWidth, delete);

            string returnPath = "";
            if (layer != null)
                returnPath = layer.FullPath;

            // Assign the created or updated full layer path to the output parameter.
            DA.SetData(0, returnPath);
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
                return Resource.UpdateLayer;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd569"); }
        }
    }
}
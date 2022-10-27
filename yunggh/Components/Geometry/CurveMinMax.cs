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

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class CurveMinMax : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CurveMinMax()
          : base("Curve Min Max", "CMM",
              "Find the minimum and maximum deviation between overlapping curves.",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve A", "A", "Curve A", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve B", "B", "Curve B", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Maximum", "MX", "Maximum Deviation", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Curve A", "MXPA", "Max Curve A Parameter", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Curve B", "MXPB", "Max Curve B Parameter", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum", "MN", "Minimum Deviation", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Curve A", "MNPA", "Min Curve A Parameter", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Curve B", "MNPB", "Min Curve B Parameter", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            Curve curveA = null;
            Curve curveB = null;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref curveA)) return;
            if (!DA.GetData(1, ref curveB)) return;

            // warnings
            if (curveA == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve A is null");
                return;
            }
            if (curveB == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve B is null");
                return;
            }

            // main
            double maxDistance;
            double maxDistanceParameterA;
            double maxDistanceParameterB;
            double minDistance;
            double minDistanceParameterA;
            double minDistanceParameterB;

            Curve.GetDistancesBetweenCurves(
                curveA, curveB, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
              out maxDistance, out maxDistanceParameterA, out maxDistanceParameterB,
              out minDistance, out minDistanceParameterA, out minDistanceParameterB
              );

            // Finally assign the calculations to the output parameters.
            DA.SetData(0, maxDistance);
            DA.SetData(1, maxDistanceParameterA);
            DA.SetData(2, maxDistanceParameterB);
            DA.SetData(3, minDistance);
            DA.SetData(4, minDistanceParameterA);
            DA.SetData(5, minDistanceParameterB);
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
                return Resource.CurveMinMax;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ad6b9dc4-3c72-4cc9-bcb6-15cba5763604"); }
        }
    }
}
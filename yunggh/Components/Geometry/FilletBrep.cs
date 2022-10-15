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
using System.Linq;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class FilletBrep : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public FilletBrep()
          : base("FilletBrep", "FBR",
              "Fillets all Brep edges with the largest possible radius in the given interval.",
              "yung gh", "Geometry")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep to fillet.", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Interval", "I", "Domain for all edge fillet radii.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step", "N", "Step size between variable fillet radii.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("TryFixEdges", "B", "Tries to fillet any failed edges with the minimum radius.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Variably filleted Brep.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            Brep InputBrep = null;
            Interval FilletRadiusInterval = new Interval();
            Double FilletRadiusStepSize = 0;
            bool TryFixEdges = false;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref InputBrep)) return;
            if (!DA.GetData(1, ref FilletRadiusInterval)) return;
            if (!DA.GetData(2, ref FilletRadiusStepSize)) return;
            if (!DA.GetData(3, ref TryFixEdges)) return;

            // warnings
            if (InputBrep == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Brep parameter is null");
                return;
            }
            if (FilletRadiusInterval == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Interval parameter is null");
                return;
            }
            if (FilletRadiusStepSize == 0 || FilletRadiusStepSize > FilletRadiusInterval.Max - FilletRadiusInterval.Min)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Radius step size must be greater than 0 and less than the radii interval domain");
                return;
            }

            // main
            Brep filletedBrep = GetFilletedBrep(InputBrep, FilletRadiusInterval, FilletRadiusStepSize, TryFixEdges);

            // Finally assign the boolean brep to the output parameter.
            DA.SetData(0, filletedBrep);
        }

        private Brep GetFilletedBrep(Brep inputBrep, Interval filletRadiusInterval, double filletRadiusStepSize, bool tryFixEdges)
        {
            // internal variables
            double minFilletRadius = (double)filletRadiusInterval.Min;
            double maxFilletRadius = (Convert.ToInt32(Math.Floor((filletRadiusInterval.Max - minFilletRadius) / filletRadiusStepSize)) * filletRadiusStepSize) + minFilletRadius;
            double tolerance = 0.01;
            Brep outputBrep = inputBrep;
            YungGH yunggh = new YungGH();

            // Find edges to fillet by omitting seam edges
            List<BrepEdge> edgesToFillet = inputBrep.Edges.ToList();
            for (int j = edgesToFillet.Count - 1; j >= 0; j--)
            {
                if (edgesToFillet[j].IsSmoothManifoldEdge()) { edgesToFillet.RemoveAt(j); }
            }

            // store list of all edge indices and fillet radii
            List<int> edgeIndices = new List<int>();
            List<double> filletRadii = new List<double>();

            // call parallel processing to find fillet radius for each edge to fillet
            Parallel.ForEach(edgesToFillet, (currentEdge) =>
            {
                // properties
                Curve thisCurve = currentEdge.EdgeCurve;
                int thisEdgeIndex = currentEdge.EdgeIndex;
                int[] adjacentFaceIndices = currentEdge.AdjacentFaces();

                // find all other edges that are adjacent to the same adjacent faces: these are potential problem edges
                List<BrepEdge> testEdges = new List<BrepEdge>();
                List<int> testEdgeIndices = new List<int>();
                foreach (BrepEdge edgeToTest in edgesToFillet)
                {
                    // test to see if this edge shares the same face
                    int[] testEdgeFaceIndices = edgeToTest.AdjacentFaces();
                    if (testEdgeFaceIndices.Intersect(adjacentFaceIndices).ToList().Count == 0) { continue; }
                    testEdges.Add(edgeToTest);
                    testEdgeIndices.Add(edgeToTest.EdgeIndex);
                }

                // iteratively try to find largest possible interval multiplier when filetting these edges
                double testRadius = maxFilletRadius;
                bool foundRadius = false;
                while (!foundRadius)
                {
                    List<double> testRadii = new List<double>();
                    foreach (int testEdgeItem in testEdgeIndices) { testRadii.Add(testRadius); }
                    Brep[] testFilletedBreps = Brep.CreateFilletEdges(inputBrep, testEdgeIndices, testRadii, testRadii, BlendType.Fillet, RailType.RollingBall, tolerance);
                    if (testFilletedBreps.Length == 0)
                    {
                        if (testRadius - filletRadiusStepSize >= minFilletRadius)
                        {
                            testRadius -= filletRadiusStepSize;
                        }
                        else
                        {
                            foundRadius = true;
                            if (tryFixEdges) { testRadius = minFilletRadius; }
                            else { testRadius = 0; }
                        }
                    }
                    else
                    {
                        foundRadius = true;
                    }
                }

                // set result properties if successful fillet operation found
                if (testRadius != 0)
                {
                    edgeIndices.Add(thisEdgeIndex);
                    filletRadii.Add(testRadius);
                }
            });

            // fillet all bredEdges with their corresponding radii
            if (edgeIndices.Count != 0 && edgeIndices.Count == filletRadii.Count)
            {
                Brep[] filletedBreps = Brep.CreateFilletEdges(inputBrep, edgeIndices, filletRadii, filletRadii, BlendType.Fillet, RailType.RollingBall, tolerance);
                if (filletedBreps.Length > 0) { outputBrep = yunggh.FindLargestBrepByVolume(filletedBreps); }
            }

            //output
            return outputBrep;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.FilletBrep;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4aa06868-b1d6-4057-915e-e3c21963bd85"); }
        }
    }
}
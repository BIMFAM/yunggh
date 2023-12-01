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
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class ContinuousPanels : GH_Component
    {
        public ContinuousPanels()
          : base("Continuous Panel", "PNL",
              "Panelizes a Polysurface such that panel direction continues across folds.",
              "yung gh", "Panelization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Facade", "B", "Polysurface to be panelized", GH_ParamAccess.item);
            pManager.AddCurveParameter("Guide Curve", "C", "Guide Curve determining panel start and direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Panel Width", "W", "Panel Width", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Panel Height", "H", "Panel Height", GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("UV Direction", "D", "Panel UV Direction", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Bond Shift", "S", "Panel Bond Shift Percentage.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Panels", "P", "Panel Breps.", GH_ParamAccess.tree);
            pManager.AddTextParameter("IDs", "ID", "Unique Identifier for each panel", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Debug.WriteLine("Solve Instance Started");
            //get inputs
            Brep facade = null;
            Curve guideCurve = null;
            double panelWidth = 1;
            double panelHeight = 1;
            bool uvDirection = true;
            double bondShift = 0;

            if (!DA.GetData(0, ref facade)) return;
            if (!DA.GetData(1, ref guideCurve)) return;
            if (!DA.GetData(2, ref panelWidth)) return;
            if (!DA.GetData(3, ref panelHeight)) return;
            if (!DA.GetData(4, ref uvDirection)) return;
            if (!DA.GetData(5, ref bondShift)) return;

            //main script
            var panels = new GH_Structure<GH_Brep>();
            var ids = new GH_Structure<GH_String>();

            //1) unroll facade
            Point3d[] points;
            Brep[] breps;
            UnrollFacade(facade, guideCurve, out points, out breps);

            var outputTesting = MultiUnroll.ConvertToGH(breps);
            panels.AppendRange(outputTesting);
            Point3d start = points[0];

            //output
            DA.SetDataTree(0, panels);
            DA.SetDataTree(1, ids);

            Debug.WriteLine("Solve Instance Ended");
        }

        private static void UnrollFacade(Brep facade, Curve guideCurve, out Point3d[] points, out Brep[] breps)
        {
            //1.1) unroll variables
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Debug.WriteLine(tol.ToString());
            Rhino.Geometry.Unroller unroll = new Rhino.Geometry.Unroller(facade);
            unroll.AbsoluteTolerance = tol;
            unroll.RelativeTolerance = tol;
            unroll.ExplodeOutput = false;

            //1.2) add guide curve as end points to unroll
            Point3d start = guideCurve.PointAtStart;
            Point3d end = guideCurve.PointAtEnd;
            unroll.AddFollowingGeometry(start);
            unroll.AddFollowingGeometry(end);

            //1.3) unroll
            Curve[] curves;
            TextDot[] dots;
            breps = unroll.PerformUnroll(out curves, out points, out dots);
            breps = Brep.JoinBreps(breps, tol);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3E74927D-C274-46C0-9329-C4ADE467B0FC"); }
        }
    }
}
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
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class UpdateCamera : GH_Component
    {
        public UpdateCamera()
          : base("Update Camera", "Update Camera",
              "Updates the viewport view.",
              "yung gh", "Document")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Viewport", "V", "Viewport Name", GH_ParamAccess.item, "Perspetive");
            pManager.AddPlaneParameter("Plane", "P", "Plane (Origin is Camera Location, YAxis is the Camera forward and ZAxis is Camera up).", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Updated", "U", "True when camera Updated", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            string viewportName = "Perspective";
            Plane plane = Plane.WorldXY;
            if (!DA.GetData(0, ref viewportName)) { }
            if (!DA.GetData(1, ref plane)) { DA.SetData(0, false); return; }

            //get Viewport to operate on
            var views = Rhino.RhinoDoc.ActiveDoc.Views.ToList();
            Rhino.Display.RhinoView view = null;
            foreach (var v in views)
            {
                string name = v.MainViewport.Name;
                if (name != viewportName) { continue; }
                view = v;
            }
            if (view == null) { DA.SetData(0, false); return; }

            //get viewport
            var viewport = view.ActiveViewport;
            if (viewport == null) { DA.SetData(0, false); return; }

            //update viewport
            viewport.SetCameraLocation(plane.Origin, true);
            viewport.SetCameraDirection(plane.YAxis, true);
            viewport.CameraUp = plane.ZAxis;
            view.Redraw();

            // Finally assign the spiral to the output parameter.
            DA.SetData(0, true);
        }

        public override GH_Exposure Exposure
        { get { return GH_Exposure.primary; } }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.UpdateCamera; } }

        public override Guid ComponentGuid
        { get { return new Guid("43d68cf2-f346-4467-a302-96162f0bd565"); } }
    }
}
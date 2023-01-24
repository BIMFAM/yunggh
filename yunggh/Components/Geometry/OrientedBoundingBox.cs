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

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace yunggh
{
    public class OrientedBoundingBox : GH_Component
    {
        public OrientedBoundingBox()
          : base("Oriented Bounding Box", "OBB",
              "Orients a bounding box for geometric fit",
              "yung gh", "Geometry")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Geometryfor bounding box", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBoxParameter("Bounding Box", "B", "Oriented bounding box", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base Plane", "P", "Base plane for bounding box", GH_ParamAccess.item);
            pManager.AddVectorParameter("Normal", "N", "Bounding box normal (up) direction", GH_ParamAccess.item);
            pManager.AddVectorParameter("Forward", "F", "Bounding box forward direction", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve all data from the input parameters (start by declaring variables and assigning them starting values).
            GeometryBase geo = null;

            // guard statement for when data cannot be extracted from a parameter
            if (!DA.GetData(0, ref geo)) return;

            //warnings
            if (geo == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Geometry is null."); return; }

            //main function

            //declare out variables
            Point3d origin = new Point3d(0, 0, 0);
            Vector3d normal = new Vector3d(0, 0, 1);
            Vector3d forward = new Vector3d(0, 0, 0);
            Plane plane = new Plane(origin, normal);

            //function
            Box box = YungGH.FitBoundingBox(geo, out plane, out normal, out forward);

            // Assign the boundingbox, plane, normal, and forward to the output parameters.
            DA.SetData(0, box);
            DA.SetData(1, plane);
            DA.SetData(2, normal);
            DA.SetData(3, forward);
        }

        public override GH_Exposure Exposure
        { get { return GH_Exposure.primary; } }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.OrientedBoundingBox; } }

        public override Guid ComponentGuid
        { get { return new Guid("696a7e35-b71a-4b25-ac5d-af54b7084ec8"); } }
    }
}
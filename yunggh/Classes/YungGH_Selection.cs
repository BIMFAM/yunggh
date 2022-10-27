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
using System.Text;
using System.Threading.Tasks;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    internal partial class YungGH
    {
        /// <summary>
        /// Selects objects in the Rhino document based on their Guids.
        /// </summary>
        /// <param name="doc">Rhino Document</param>
        /// <param name="guids">List of guids to select</param>
        public static void Select(RhinoDoc doc, List<System.Guid> guids)
        {
            foreach (System.Guid guid in guids)
                doc.Objects.Select(guid, true, true, true);
        }

        /// <summary>
        /// Selects all objects on a layer
        /// </summary>
        /// <param name="doc">Rhino document</param>
        /// <param name="layer">Full layer path</param>
        /// <returns>A list of selected Guids</returns>
        public List<System.Guid> SelectObjectsByLayer(RhinoDoc doc, Rhino.DocObjects.Layer layer)
        {
            List<System.Guid> guids = new List<System.Guid>();

            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer);
            foreach (Rhino.DocObjects.RhinoObject rhobj in rhobjs)
            {
                doc.Objects.Select(rhobj.Id, true, true, true);
                guids.Add(rhobj.Id);
            }

            return guids;
        }
    }
}
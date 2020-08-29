using System;
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
    partial class YungGH
    {
        /// <summary>
        /// Selects objects in the Rhino document based on their Guids.
        /// </summary>
        /// <param name="doc">Rhino Document</param>
        /// <param name="guids">List of guids to select</param>
        public void Select(RhinoDoc doc, List<System.Guid> guids)
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
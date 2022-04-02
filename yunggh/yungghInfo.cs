using System;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI.Canvas;

namespace yunggh
{
    public class yungghInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "yung GH";
            }
        }

        public override Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        public override string Description
        {
            get
            {
                return "This library open source library contains useful Grasshopper components.";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("51c6e8fa-ee0c-43b7-abaf-419b24603c29");
            }
        }

        public override string AuthorName
        {
            get
            {
                return "archgame";
            }
        }

        public override string AuthorContact
        {
            get
            {
                return "@archgame";
            }
        }
    }

    public class yungghCategoryIcon : Grasshopper.Kernel.GH_AssemblyPriority
    {
        public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("yung GH", Resource.yunggh);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("yung GH", '¥');
            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }

    //TODO: YungGH into partial classes - Data, Geometry, Document, Select, Format, Mesh
    //TODO: CombineWorksession
    //TODO: Export, add geometry bake and remove
    //TODO: Import
    //TODO: ReadAttributes, graft name output, simplify tree
    //TODO: RestartComponent
    //TODO: SurfaceFit
    //TODO: SelectLayer, add selected boolean
    //TODO: UpdateCamera
    //TODO: ViewportCapture
    //TODO: CreateSavedView
    //TODO: Flatten Sort Pick
    //TODO: Delay
    //TODO: Listener
    //TODO: IFC Import
    //TODO: IFC Export
    //TODO: IFC Geometry
    //TODO: 4D Cube
    //TODO: Deselect
    //TODO: Deselect All
    //TODO: split "auto select" and "prompt select"
    //TODO: Setup Inno
    //TODO: SplitKeepRemove turns error curves into planes if planar, works without points
    //TODO: OrientedBoundingBox, turn into function
    //TODO: Best Fit BoundingBox (Volume, Area, Length)
    //TODO: Developability, add max and min curvature
    //TODO: SelectPoint, SelectGeometry, SelDup, SelDupAll turn into function
    //TODO: SelectPoint, expand to select filtered geometry, prompt
    //TODO: Unroll Brep
    //TODO: Layout Pieces (fabrication tab)
    //TODO: Read JSON
    //TODO: Write JSON
    //TODO: Read XML
    //TODO: Write XML
    //TODO: Read Excel (OpenXML)
    //TODO: Write Excel (OpenXML)
    //TODO: Convert - CSV, Excel, JSON, XML
    //TODO: Solid Thicken (constant offset)
    //TODO: TBone (fabrication)
    //TODO: Box Packing (fabrication)
    //TODO: 3D Convex Hull
    //TODO: Isovist
    //TODO: Waffle Structure (fabrication) - solids, surfaces, single surface
    //TODO: Generate Site Model
    //TODO: Curve Boolean (keeps internal curves)
    //TODO: Developable Surface Generation
    //TODO: Method for turning DataTree into separate list for threading
    //TODO: Patterning From planes
}
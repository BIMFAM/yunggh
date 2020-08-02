using System;
using System.Drawing;
using Grasshopper.Kernel;

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
    //TODO: split auto select and prompt select
    //TODO: Setup Inno
    //TODO: SplitKeepRemove turns error curves into planes if planar
}
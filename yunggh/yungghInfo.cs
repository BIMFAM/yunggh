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

    public class CustomAttributes : GH_ComponentAttributes
    {
        public CustomAttributes(IGH_Component component)
          : base(component)
        { }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Objects)
            {
                // Cache the existing style.
                GH_PaletteStyle normalStyle = GH_Skin.palette_normal_standard;
                GH_PaletteStyle hiddenStyle = GH_Skin.palette_hidden_standard;

                //create colors
                Color pink = Color.FromArgb(255, 132, 241);
                Color darkblue = Color.FromArgb(0, 16, 87);
                Color cyan = Color.FromArgb(134, 200, 251);

                // Swap out palette for normal, unselected components.
                GH_Skin.palette_normal_standard = new GH_PaletteStyle(pink, darkblue, darkblue);
                GH_Skin.palette_hidden_standard = new GH_PaletteStyle(cyan, darkblue, darkblue);

                base.Render(canvas, graphics, channel);

                // Put the original style back.
                GH_Skin.palette_normal_standard = normalStyle;
                GH_Skin.palette_hidden_standard = normalStyle;
            }
            else
                base.Render(canvas, graphics, channel);
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
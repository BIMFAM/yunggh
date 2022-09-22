using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    public class TextCurve : GH_Component
    {
        public TextCurve()
          : base("Text Curve", "TextCurve",
              "Turn Text in Curves.",
              "yung gh", "Format")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "Text.", GH_ParamAccess.tree);
            pManager.AddPointParameter("Points", "P", "Text Points.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Font", "F", "Font.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bold", "B", "Bold", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Italics", "I", "Italics", GH_ParamAccess.item);
            pManager.AddNumberParameter("Size", "S", "Size", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Text Curves.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //get inputs
            GH_Structure<GH_String> content = new GH_Structure<GH_String>();
            GH_Structure<GH_Point> points = new GH_Structure<GH_Point>();
            string face = "";
            bool bold = false;
            bool italics = false;
            double size = 1;
            if (!DA.GetDataTree(0, out content)) return;
            if (!DA.GetDataTree(1, out points)) return;
            if (!DA.GetData(2, ref face)) return;
            if (!DA.GetData(3, ref bold)) return;
            if (!DA.GetData(4, ref italics)) return;
            if (!DA.GetData(5, ref size)) return;

            //setup output data structures
            GH_Structure<GH_Curve> textCurves = new GH_Structure<GH_Curve>();

            //main method
            //guard statement
            if (content.DataCount == 0 || points.DataCount == 0) { return; }
            if (content.DataCount != points.DataCount) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point and Text Count don't match"); return; }
            if (content.Branches.Count != points.Branches.Count) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Point and Text Branch Count don't match"); return; }

            //set size
            if (size == 0) { size = 5; }

            //setup font
            Rhino.DocObjects.Font.FontStyle fontStyle = Rhino.DocObjects.Font.FontStyle.Upright;
            if (italics) fontStyle = Rhino.DocObjects.Font.FontStyle.Italic;
            Rhino.DocObjects.Font.FontWeight fontWeight = Rhino.DocObjects.Font.FontWeight.Normal;
            if (bold) fontWeight = Rhino.DocObjects.Font.FontWeight.Bold;
            bool underlined = false;
            bool strikethrough = false;
            Rhino.DocObjects.Font font = new Rhino.DocObjects.Font(face, fontWeight, fontStyle, underlined, strikethrough);

            //create text objects
            for (int p = 0; p < content.Branches.Count; p++)
            {
                GH_Path path = content.get_Path(p);
                List<GH_Point> pts = points.get_Branch(p).Cast<GH_Point>().ToList();
                List<GH_String> contents = content.get_Branch(p).Cast<GH_String>().ToList();

                //get text curves
                List<Curve> curves = new List<Curve>();
                for (int i = 0; i < contents.Count; i++)
                {
                    //get data
                    string text = "";
                    if (!GH_Convert.ToString_Primary(contents[i], ref text)) { continue; }
                    Point3d origin = Point3d.Unset;
                    if (!GH_Convert.ToPoint3d_Primary(pts[i], ref origin)) { continue; }
                    Plane plane = new Plane(origin, Vector3d.ZAxis);

                    //create text objects
                    TextEntity text_entity = new TextEntity
                    {
                        Plane = plane,
                        PlainText = text,
                        Justification = TextJustification.MiddleCenter,
                        Font = font
                    };

                    //set text object properties
                    Rhino.DocObjects.DimensionStyle dimstyle = new Rhino.DocObjects.DimensionStyle();
                    dimstyle.TextHeight = size;
                    double smallCapsScale = 1;
                    double spacing = 0;

                    //get curve from text object
                    Brep[] breps = text_entity.CreateSurfaces(dimstyle, smallCapsScale, spacing);
                    foreach (Brep brep in breps)
                    {
                        Curve[] edges = brep.DuplicateEdgeCurves();
                        edges = Curve.JoinCurves(edges);
                        curves.AddRange(edges);
                    }
                }

                //convert and add to output
                List<GH_Curve> curvesGH = MultiUnroll.ConvertToGH(curves);
                textCurves.AppendRange(curvesGH, path);
            }

            //output
            DA.SetDataTree(0, textCurves); //I
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.TextCurve;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9FD58061-12E4-4A70-BD54-4F359613F9C8"); }
        }
    }
}
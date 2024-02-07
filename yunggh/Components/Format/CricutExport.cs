// Copyright (c) 2023 archgame
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

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Text;
using System.IO;
using System.Linq;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace yunggh
{
    public class CricutExport : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CricutExport()
          : base("CricutExport", "Cricut",
              "Export geometry by layer as *.svg for Cricut",
              "yung gh", "Format")
        {
        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon
        /// will appear. There are seven possible locations (primary to septenary),
        /// each of which can be combined with the GH_Exposure.obscure flag, which
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.Cricut;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D784BC41-52E4-41C5-991A-6E69B24325C0"); }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Export", "E", "Run Export", GH_ParamAccess.item);
            pManager.AddTextParameter("Filepaths", "F", "Export Filepath", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curve Cut Pattern", "CB", "Curves to Cut", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curve Perforate Pattern", "CP", "Curves to Perforate", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curve Draw Pen", "DP", "Curves to Draw", GH_ParamAccess.tree);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Exported Files", "E", "Exported Filepaths", GH_ParamAccess.tree);
        }

        private bool pending = false;

        private GH_Structure<GH_String> filePaths;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Get input data from Grasshopper
            bool run = false;
            GH_Structure<GH_String> inputFilePaths;
            GH_Structure<GH_Curve> CrvPatCut;
            GH_Structure<GH_Curve> CrvPatPrf;
            GH_Structure<GH_Curve> CrvDrwPen;
            if (!DA.GetData(0, ref run)) return;
            if (!DA.GetDataTree<GH_String>(1, out inputFilePaths)) return;
            DA.GetDataTree<GH_Curve>(2, out CrvPatCut);
            DA.GetDataTree<GH_Curve>(3, out CrvPatPrf);
            DA.GetDataTree<GH_Curve>(4, out CrvDrwPen);

            // main

            //button press pattern
            if (filePaths != null) { DA.SetDataTree(0, filePaths); }
            if (!run && !pending) return; //return when button isn't pressed
            if (!pending) { pending = true; return; } //return & set pending to true
            pending = false; // reset pending to false

            //establish a clean data tree
            filePaths = new GH_Structure<GH_String>();

            //loop through all the input filepath paths
            foreach (GH_Path path in inputFilePaths.Paths)
            {
                //export objects
                GH_String[] filepaths = inputFilePaths[path].ToArray();
                for (int j = 0; j < filepaths.Length; j++)
                {
                    //get values
                    string filename = filepaths[j].Value;
                    var cuts = new List<Curve>();
                    var perfs = new List<Curve>();
                    var draws = new List<Curve>();
                    if (CrvPatCut.PathExists(path))
                    {
                        cuts = IGooArrayToCurveList(CrvPatCut[path].ToArray());
                    }
                    if (CrvPatPrf.PathExists(path))
                    {
                        perfs = IGooArrayToCurveList(CrvPatPrf[path].ToArray());
                    }
                    if (CrvDrwPen.PathExists(path))
                    {
                        draws = IGooArrayToCurveList(CrvDrwPen[path].ToArray());
                    }

                    //main function
                    ExportSVG(filename, cuts, perfs, draws);

                    //update output list
                    GH_Path newPath = new GH_Path(path);
                    filePaths.AppendRange(new List<GH_String>() { new GH_String(filename) }, newPath);
                }
            }

            // Assign the export filepaths to the output parameter.
            DA.SetDataTree(0, filePaths);
        }

        public static List<Curve> IGooArrayToCurveList(IGH_Goo[] goos)
        {
            var crvs = new List<Curve>();
            if (goos.Length == 0) { return crvs; }
            foreach (IGH_Goo goo in goos)
            {
                Curve crv = null;
                if (!GH_Convert.ToCurve_Primary(goo, ref crv)) { continue; }
                crvs.Add(crv);
            }
            return crvs;
        }

        #region SVG Methods

        private static List<string> CurvesToSVG(List<Curve> crvs, string style)
        {
            List<string> svgs = new List<string>();//{style,style,style};
            if (crvs == null) { return svgs; }
            foreach (Curve crv in crvs)
            {
                if (crv.IsLinear())
                {
                    svgs.Add(CurveToSVG(new Line(crv.PointAtStart, crv.PointAtEnd), style));
                    continue;
                }
                else if (crv.IsPolyline())
                {
                    Polyline polyline;
                    double[] parameters;
                    if (!crv.TryGetPolyline(out polyline, out parameters)) { continue; }
                    svgs.Add(CurveToSVG(polyline, style));
                    continue;
                }
                else if (crv.IsCircle())
                {
                    Circle circle;
                    if (!crv.TryGetCircle(out circle)) { continue; }
                    svgs.Add(CurveToSVG(circle, style));
                    continue;
                }
                var nurbs = crv.ToNurbsCurve();
                svgs.Add(CurveToSVG(nurbs, style));
            }
            return svgs;
        }

        private static string CurveToSVG(Line line, string style)
        {
            StringBuilder sb = new StringBuilder(_line_svg);
            //is there a translation for scale that needs to happen?
            sb.Replace("STYLE", style);
            sb.Replace("X1", (line.PointAt(0).X * scale).ToString());
            sb.Replace("Y1", (line.PointAt(0).Y * scale).ToString());
            sb.Replace("X2", (line.PointAt(1).X * scale).ToString());
            sb.Replace("Y2", (line.PointAt(1).Y * scale).ToString());
            return sb.ToString();
        }

        private static string CurveToSVG(Polyline pLine, string style)
        {
            //convert polyline to points
            StringBuilder pts = new StringBuilder("");
            foreach (Point3d pt in pLine)
            {
                pts.Append((pt.X * scale).ToString());
                pts.Append(",");
                pts.Append((pt.Y * scale).ToString());
                pts.Append(" ");
            }

            StringBuilder sb = new StringBuilder(_polyline_svg);
            //is there a translation for scale that needs to happen?
            sb.Replace("STYLE", style);
            sb.Replace("PTS", pts.ToString());
            return sb.ToString();
        }

        private static string CurveToSVG(Circle circle, string style)
        {
            StringBuilder sb = new StringBuilder(_circle_svg);
            //is there a translation for scale that needs to happen?
            sb.Replace("STYLE", style);
            sb.Replace("CX", (circle.Center.X * scale).ToString());
            sb.Replace("CY", (circle.Center.Y * scale).ToString());
            sb.Replace("R", (circle.Radius * scale).ToString());
            return sb.ToString();
        }

        public static string CurveToSVG(NurbsCurve nurbsCurve, string style)
        {
            //convert polyline to points
            StringBuilder pts = new StringBuilder("");
            /*/
            foreach(Point3d pt in pLine)
            {
              pts.Append((pt.X * scale).ToString());
              pts.Append(",");
              pts.Append((pt.Y * scale).ToString());
              pts.Append(" ");
            }
            //*/

            StringBuilder sb = new StringBuilder(_path_svg);
            //is there a translation for scale that needs to happen?
            sb.Replace("STYLE", style);
            sb.Replace("SX", (nurbsCurve.PointAtStart.X * scale).ToString());
            sb.Replace("SY", (nurbsCurve.PointAtStart.Y * scale).ToString());
            sb.Replace("PTS", pts.ToString());
            return sb.ToString();
        }

        private static void ExportSVG(string filename, List<Curve> cuts, List<Curve> perfs, List<Curve> draws)
        {
            List<string> drawsSVG = CurvesToSVG(draws, "DP");
            List<string> cutsSVG = CurvesToSVG(cuts, "CB");
            List<string> perfsSVG = CurvesToSVG(perfs, "CP");

            List<string> exportSVG = new List<string>();

            exportSVG.Add(_template_svg[0]);
            exportSVG.Add(_template_svg[1]);
            exportSVG.Add(_template_svg[2]);
            exportSVG.Add(_template_svg[3]);
            exportSVG.Add(_template_svg[4]);
            exportSVG.Add(_template_svg[5]);
            exportSVG.Add(_template_svg[6]);
            exportSVG.Add(_template_svg[7]);
            exportSVG.Add(_template_svg[8]);
            exportSVG.Add(_template_svg[9]);
            exportSVG.AddRange(cutsSVG);
            exportSVG.Add(_template_svg[11]);
            exportSVG.Add(_template_svg[12]);
            exportSVG.AddRange(perfsSVG);
            exportSVG.Add(_template_svg[14]);
            exportSVG.Add(_template_svg[15]);
            exportSVG.AddRange(drawsSVG);
            exportSVG.Add(_template_svg[17]);
            exportSVG.Add(_template_svg[18]);

            File.WriteAllLines(filename, exportSVG);
        }

        //nurbs cuves to path
        //https://discourse.mcneel.com/t/export-to-svg-challenge/25618/12

        //private string _style_svg = "";
        private const string _line_svg = "    <line class=\"STYLE\" x1=\"X1\" y1=\"Y1\" x2=\"X2\" y2=\"Y2\"/>";

        private const string _path_svg = "    <path class=\"STYLE\" d=\"MSX,SYcPTS\"/>";//"    <path class=\"STYLE\" d=\"M306,492c144,0,144-72,288-72\"/>";
        private const string _polyline_svg = "    <polyline class=\"STYLE\" points=\"PTS 	\"/>";
        private const string _circle_svg = "    <circle class=\"STYLE\" cx=\"CX\" cy=\"CY\" r=\"R\"/>";
        private const int scale = 72;

        private static List<string> _template_svg = new List<string>()
    {
      "<?xml version=\"1.0\" encoding=\"utf-8\"?>", //0
      "<!-- Generator: yunggh, Rhino Circut Export. SVG Version: 6.00 Build 0)  -->", //1
      @"<svg version=""1.0"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" x=""0px"" y=""0px""", //2
      "   viewBox=\"0 0 612 792\" style=\"enable-background:new 0 0 612 792;\" xml:space=\"preserve\">", //3, these viewbox numbers might need to change, unsure
      "<style type=\"text/css\">",//4
      "   .cb{fill:none;stroke:#FF0000;stroke-linecap:round;stroke-linejoin:round;}",//5
      "   .cp{fill:none;stroke:#00FF00;stroke-linecap:round;stroke-linejoin:round;}",//6
      "   .dp{fill:none;stroke:#00ffff;stroke-linecap:round;stroke-linejoin:round;}",//7
      "</style>",//8
      "<g id=\"CUT_BASIC\">",//9
      "	CutBasicCurves",//10
      "</g>",//11
      "<g id=\"CUT_PERFORATE\">",//12
      " CutPerforateCurves",//13
      "</g>",//14
      "<g id=\"DRAW_PEN\">",//15
      " DrawPenCurves",//16
      "</g>",//17
      "</svg>"//18
      };

        #endregion SVG Methods
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Render.ChangeQueue;

namespace yunggh.Components.Panelization
{
    public class PyramidPanelization : PanelizationBase
    {
        #region UI

        public PyramidPanelization()
          : base("Pyramid Panelization", "PYRPNL", "Panelize Surface with Pyramid type panelization method.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("D1F8782F-F468-4C25-AB97-B7791C587C83"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddNumberParameter("Height", "H", "Pyramid Height", GH_ParamAccess.item);
            pManager.AddNumberParameter("Truncation", "T", "Normalized Pyramid Truncation Location", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        #endregion UI

        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            //get inputs
            var height = 1.0;
            var truncation = 1.0;
            DA.GetData(5, ref height);
            DA.GetData(6, ref truncation);

            //create pyramid panels
            var panels = GetPyramidPanels(quads, brep, height, truncation);

            return panels;
        }

        public static List<List<List<Brep>>> GetPyramidPanels(List<List<List<Point3d>>> quadsByRow, Brep brep, double height, double truncation)
        {
            var output = new List<List<List<Brep>>>();

            for (int r = 0; r < quadsByRow.Count; r++)
            {
                var rowOutput = new List<List<Brep>>();
                var row = quadsByRow[r];
                for (int j = 0; j < row.Count; j++)
                {
                    var pyramidOutput = new List<Brep>();
                    var quad = row[j];
                    //TODO: test if quad is a pentagon
                    //test if quad is a triangle
                    if (quad[0] == Point3d.Unset || quad[1] == Point3d.Unset || quad[2] == Point3d.Unset || quad[3] == Point3d.Unset)
                    {
                        //find out how many points are unset
                        int unsetPtCount = 0;
                        foreach (var pt in quad)
                        {
                            if (pt != Point3d.Unset) { continue; }
                            unsetPtCount++;
                        }
                        if (unsetPtCount > 1) { continue; } //continue if not enough points for triangle

                        //create single quad triangle panel
                        //Debug.WriteLine("D" + r + "-" + j);
                        if (quad[0] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[1], quad[2], quad[3]);
                            if (pnl == null) { continue; }
                            pyramidOutput.Add(pnl.ToBrep());
                        }
                        else if (quad[1] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[2], quad[3]);
                            if (pnl == null) { continue; }
                            pyramidOutput.Add(pnl.ToBrep());
                        }
                        else if (quad[2] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[1], quad[3]);
                            if (pnl == null) { continue; }
                            pyramidOutput.Add(pnl.ToBrep());
                        }
                        else if (quad[3] == Point3d.Unset)
                        {
                            var pnl = NurbsSurface.CreateFromCorners(quad[0], quad[1], quad[2]);
                            if (pnl == null) { continue; }
                            pyramidOutput.Add(pnl.ToBrep());
                        }
                        //Debug.WriteLine("C" + r + "-" + j);
                        continue; //continue because quad is triangle
                    }

                    //get pyramid tip
                    var center = (quad[0] + quad[1] + quad[2] + quad[3]) / 4.00;
                    var srf = brep.Surfaces[0];
                    double u, v;
                    srf.ClosestPoint(center, out u, out v);
                    var normal = srf.NormalAt(u, v);
                    center += normal * height;

                    //create pyramid panels
                    quad.RemoveAt(quad.Count - 1); //at this point we assume there is no need for a pentagon
                    for (int i = 1; i <= quad.Count; i++)
                    {
                        var pt0 = quad[i - 1];
                        var pt1 = Point3d.Unset;
                        if (i == quad.Count)
                        {
                            pt1 = quad[0];
                        }
                        else
                        {
                            pt1 = quad[i];
                        }
                        //Debug.WriteLine("A" + r + "-" + j + "-" + i);
                        var pnl = NurbsSurface.CreateFromCorners(pt0, pt1, center);
                        if (pnl == null) { continue; }
                        //Debug.WriteLine("B");
                        pyramidOutput.Add(pnl.ToBrep());
                        //Debug.WriteLine("C");
                    }

                    rowOutput.Add(pyramidOutput);
                }

                output.Add(rowOutput);
            }

            return output;
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            //var sortedPanels = SortDiagonally(panels);
            //return ListListToTree(sortedPanels);
            return ListListListToTree(panels);
        }

        public static List<List<Brep>> SortDiagonally(List<List<List<Brep>>> inputList)
        {
            int rowCount = inputList.Count;
            int colCount = inputList.Max(row => row.Count);

            // Initialize the outputList
            var outputList = new List<List<Brep>>();

            // Iterate through diagonals starting from the top-left corner
            for (int diagonalSum = 0; diagonalSum < rowCount + colCount - 1; diagonalSum++)
            {
                var diagonal = new List<Brep>();

                // Iterate through rows of the inputList
                for (int row = 0; row < rowCount; row++)
                {
                    int col = diagonalSum - row;

                    // If the column index is within the bounds of the current row
                    if (col >= 0 && col < inputList[row].Count)
                    {
                        // Treat the first two elements of each inner list separately
                        if (col < 2)
                        {
                            // Add the first element (index 0) to the diagonal list
                            diagonal.Add(inputList[row][col][0]);

                            // Add the second element (index 1) to the diagonal list
                            diagonal.Add(inputList[row][col][1]);
                        }
                        else
                        {
                            // Add the elements 2 and 3 to the other list
                            diagonal.AddRange(inputList[row][col].Skip(2));
                        }
                    }
                }

                // Add the diagonal list to the outputList
                outputList.Add(diagonal);
            }

            return outputList;
        }
    }
}
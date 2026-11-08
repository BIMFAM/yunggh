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

            //NOTE: the quad corner grid isn't used here. Like the original definition,
            //the surface is split by the U and V curves and each face is panelized
            //from its own boundary corners
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            DA.GetDataList(1, uCrvs);
            DA.GetDataList(2, vCrvs);
            var cutters = uCrvs.Concat(vCrvs).Where(c => c != null).ToList();

            //create pyramid panels
            var panels = GetPyramidPanels(brep, cutters, height, truncation);

            return panels;
        }

        public static List<List<List<Brep>>> GetPyramidPanels(Brep brep, List<Curve> cutters, double height, double truncation)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc != null ? Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance : 0.001;
            var output = new List<List<List<Brep>>>();
            var row = new List<List<Brep>>();
            output.Add(row);
            if (brep == null || brep.Faces.Count == 0) { return output; }

            //split surface into faces
            var baseFace = brep.Faces[0];
            var split = baseFace.Split(cutters, tol);
            if (split == null) { return output; }

            foreach (var face in split.Faces)
            {
                var cell = new List<Brep>();
                var corners = GetFaceCorners(face, tol);

                if (corners.Count == 4)
                {
                    cell.AddRange(GetPyramid(face, corners, height, truncation));
                }
                else if (corners.Count >= 3)
                {
                    //not a quad, so make a flat panel (like the Delaunay branch of the definition)
                    cell.AddRange(GetFlatPanel(corners, tol));
                }

                row.Add(cell);
            }

            return output;
        }

        private static List<Point3d> GetFaceCorners(BrepFace face, double tol)
        {
            var corners = new List<Point3d>();
            var loop = face.OuterLoop;
            if (loop == null) { return corners; }
            var boundary = loop.To3dCurve();
            if (boundary == null) { return corners; }

            //explode boundary into segments, a corner is the start of each segment
            var segments = boundary.DuplicateSegments();
            if (segments == null || segments.Length == 0) { segments = new Curve[] { boundary }; }
            foreach (var seg in segments)
            {
                var pt = seg.PointAtStart;
                if (corners.Count > 0 && corners[corners.Count - 1].DistanceTo(pt) < tol) { continue; }
                corners.Add(pt);
            }
            if (corners.Count > 1 && corners[0].DistanceTo(corners[corners.Count - 1]) < tol)
            {
                corners.RemoveAt(corners.Count - 1);
            }
            return corners;
        }

        private static List<Brep> GetPyramid(BrepFace face, List<Point3d> corners, double height, double truncation)
        {
            var output = new List<Brep>();

            //flat base through the 4 corners
            var baseSrf = NurbsSurface.CreateFromCorners(corners[0], corners[1], corners[2], corners[3]);
            if (baseSrf == null) { return output; }

            //get base normal at the center of the corners
            var center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4.00;
            double u, v;
            baseSrf.ClosestPoint(center, out u, out v);
            var normal = baseSrf.NormalAt(u, v);

            //make sure the normal agrees with the surface being panelized
            double fu, fv;
            face.ClosestPoint(center, out fu, out fv);
            var faceNormal = face.NormalAt(fu, fv);
            if (face.OrientationIsReversed) { faceNormal.Reverse(); }
            if (normal * faceNormal < 0) { normal.Reverse(); }

            //get pyramid tip
            normal.Unitize();
            var tip = center + normal * height;

            //no pyramid, just the flat base
            if (truncation <= 0 || height == 0)
            {
                output.Add(baseSrf.ToBrep());
                return output;
            }
            if (truncation > 1) { truncation = 1; }

            //truncated points along each corner-to-tip edge
            var truncated = corners.Select(c => c + (tip - c) * truncation).ToList();
            bool isTruncated = truncation < 1;

            //create pyramid panels
            for (int i = 0; i < 4; i++)
            {
                var pt0 = corners[i];
                var pt1 = corners[(i + 1) % 4];
                NurbsSurface pnl;
                if (isTruncated)
                {
                    pnl = NurbsSurface.CreateFromCorners(pt0, pt1, truncated[(i + 1) % 4], truncated[i]);
                }
                else
                {
                    pnl = NurbsSurface.CreateFromCorners(pt0, pt1, tip);
                }
                if (pnl == null) { continue; }
                output.Add(pnl.ToBrep());
            }

            return output;
        }

        private static List<Brep> GetFlatPanel(List<Point3d> corners, double tol)
        {
            var output = new List<Brep>();
            if (corners.Count == 3)
            {
                var tri = NurbsSurface.CreateFromCorners(corners[0], corners[1], corners[2]);
                if (tri != null) { output.Add(tri.ToBrep()); }
                return output;
            }

            //triangulate the polygon and join the triangles
            var pts = new List<Point3d>(corners) { corners[0] };
            var mesh = Rhino.Geometry.Mesh.CreateFromClosedPolyline(new Polyline(pts));
            if (mesh == null) { return output; }
            var triangles = new List<Brep>();
            foreach (var f in mesh.Faces)
            {
                var srf = NurbsSurface.CreateFromCorners(mesh.Vertices[f.A], mesh.Vertices[f.B], mesh.Vertices[f.C]);
                if (srf != null) { triangles.Add(srf.ToBrep()); }
            }
            var joined = Brep.JoinBreps(triangles, tol);
            output.AddRange(joined ?? triangles.ToArray());
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
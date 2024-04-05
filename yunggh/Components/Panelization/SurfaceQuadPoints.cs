using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class SurfaceQuadPoints : GH_Component
    {
        #region UI

        public SurfaceQuadPoints()
          : base("Surface Quad Corners", "Quads",
              "Get quad corners for a surface.",
              "yung gh", "Panelization")
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("8D9E1853-85F3-4ADA-93DB-769AA775C537"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("U Curves", "U", "'U' Curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("V Curves", "V", "'V' Curves", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Flip U Curves", "FU", "Flip 'U' Curves", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip V Curves", "FV", "Flip 'V' Curves", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Quad Points", "P", "Data tree with Quad Points", GH_ParamAccess.tree);
        }

        #endregion UI

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //input
            var uCrvs = new List<Curve>();
            var vCrvs = new List<Curve>();
            var uFlip = false;
            var vFlip = false;
            if (!DA.GetDataList(0, uCrvs)) return;
            if (!DA.GetDataList(1, vCrvs)) return;
            if (!DA.GetData(2, ref uFlip)) return;
            if (!DA.GetData(3, ref vFlip)) return;

            //main process
            var uCrvsSorted = new List<Curve>();
            var vCrvsSorted = new List<Curve>();
            var uIndicesSorted = new List<int>();
            var vIndicesSorted = new List<int>();
            SortCurvesBySurface.Sort(uCrvs, vCrvs, uFlip, vFlip
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //get quad corners
            var quads = GetQuadCorners(uCrvsSorted, vCrvsSorted);

            //output
            var quadTree = ToDataTree(quads);
            DA.SetDataTree(0, quadTree);
        }

        public static List<List<List<Point3d>>> GetQuadCorners(List<Curve> uCrvs, List<Curve> vCrvs)
        {
            //setting up a complete grid with empty point values
            //generating default quad list structure by row
            int uCrvCount = uCrvs.Count + 1;
            int vCrvCount = vCrvs.Count + 1;
            var output = new List<List<List<Point3d>>>();
            for (int u = 0; u < uCrvCount; u++)
            {
                var quadRow = new List<List<Point3d>>();
                for (int v = 0; v < vCrvCount; v++)
                {
                    var quad = new List<Point3d>();
                    for (int i = 0; i < 5; i++)
                    {
                        quad.Add(Point3d.Unset);
                    }
                    quadRow.Add(quad);
                }
                output.Add(quadRow);
            }

            //get topmost and bottom most rows

            //for each quad, find points
            for (int u = 1; u < output.Count - 1; u++)
            {
                var quadRow = output[u];
                for (int v = 1; v < quadRow.Count - 1; v++)
                {
                    var quad = quadRow[v];
                    var topUCrv = uCrvs[u];
                    var botUCrv = uCrvs[u - 1];
                    var leftVCrv = vCrvs[v - 1];
                    var rightVCrv = vCrvs[v];

                    var topLeft = GetIntersection(topUCrv, leftVCrv);
                    var topRight = GetIntersection(topUCrv, rightVCrv);
                    var botLeft = GetIntersection(botUCrv, leftVCrv);
                    var botRight = GetIntersection(botUCrv, rightVCrv);
                    quad[0] = topLeft;
                    quad[1] = topRight;
                    quad[2] = botRight;
                    quad[3] = botLeft;

                    quad = FixQuad(quad, topUCrv, botUCrv, leftVCrv, rightVCrv);
                    

                    quadRow[v] = quad;
                }
                output[u] = quadRow;
            }

            //find any pentagons

            return output;
        }

        private static List<Point3d> FixQuad(List<Point3d> quad, Curve topUCrv, Curve botUCrv, Curve leftVCrv, Curve rightVCrv)
        {
            //guard statement
            if (quad[0] == Point3d.Unset && quad[1] == Point3d.Unset && quad[2] == Point3d.Unset && quad[3] == Point3d.Unset) { return quad; }
            if (quad[0] != Point3d.Unset && quad[1] != Point3d.Unset && quad[2] != Point3d.Unset && quad[3] != Point3d.Unset) { return quad; }

            #region One Point Missing
            //top left is missing
            if (quad[0] == Point3d.Unset && quad[1] != Point3d.Unset && quad[2] != Point3d.Unset && quad[3] != Point3d.Unset)
            {
                quad[0] = topUCrv.PointAtStart;
                return quad;
            }
            //bottom left is missing
            if (quad[0] != Point3d.Unset && quad[1] != Point3d.Unset && quad[2] != Point3d.Unset && quad[3] == Point3d.Unset)
            {
                quad[3] = botUCrv.PointAtStart;
                return quad;
            }
            //top right is missing
            if (quad[0] != Point3d.Unset && quad[1] == Point3d.Unset && quad[2] != Point3d.Unset && quad[3] != Point3d.Unset)
            {
                quad[1] = topUCrv.PointAtEnd;
                return quad;
            }
            //bottom right is missing
            if (quad[0] != Point3d.Unset && quad[1] != Point3d.Unset && quad[2] == Point3d.Unset && quad[3] != Point3d.Unset)
            {
                quad[2] = botUCrv.PointAtEnd;
                return quad;
            }
            #endregion

            #region Two Points Missing
            //left is missing
            if (quad[0] == Point3d.Unset && quad[1] != Point3d.Unset && quad[2] != Point3d.Unset && quad[3] == Point3d.Unset) 
            {
                quad[0] = topUCrv.PointAtStart;
                quad[3] = botUCrv.PointAtStart;
                return quad; 
            }
            //right is missing
            if (quad[0] != Point3d.Unset && quad[1] == Point3d.Unset && quad[2] == Point3d.Unset && quad[3] != Point3d.Unset) 
            {
                quad[1] = topUCrv.PointAtEnd;
                quad[2] = botUCrv.PointAtEnd;
                return quad; 
            }
            //top is missing
            if (quad[0] == Point3d.Unset && quad[1] == Point3d.Unset && quad[2] != Point3d.Unset && quad[3] != Point3d.Unset) 
            {
                quad[0] = leftVCrv.PointAtEnd;
                quad[1] = rightVCrv.PointAtEnd;
                return quad; 
            }
            //bottom is missing
            if (quad[0] != Point3d.Unset && quad[1] != Point3d.Unset && quad[2] == Point3d.Unset && quad[3] == Point3d.Unset) 
            {
                quad[2] = rightVCrv.PointAtStart;
                quad[3] = leftVCrv.PointAtStart;
                return quad; 
            }
            #endregion

            //only top left
            if (quad[0] == Point3d.Unset)
            {
                quad[1] = topUCrv.PointAtEnd;
                quad[3] = leftVCrv.PointAtStart;
                return quad;
            }
            //only top right
            if (quad[1] == Point3d.Unset)
            {
                quad[0] = topUCrv.PointAtStart;
                quad[2] = rightVCrv.PointAtStart;
                return quad;
            }
            //only bottom right
            if (quad[2] == Point3d.Unset)
            {
                quad[1] = rightVCrv.PointAtEnd;
                quad[3] = botUCrv.PointAtStart;
                return quad;
            }
            //only bottom left
            if (quad[3] == Point3d.Unset)
            {
                quad[0] = leftVCrv.PointAtEnd;
                quad[2] = botUCrv.PointAtEnd;
                return quad;
            }
            return quad;
        }

        private static Point3d GetIntersection(Curve A, Curve B)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var crvXs = Rhino.Geometry.Intersect.Intersection.CurveCurve(A, B, tol, tol);
            if (crvXs.Count == 0) { return Point3d.Unset; }
            return crvXs[0].PointA;
        }

        public static GH_Structure<GH_Point> ToDataTree(List<List<List<Point3d>>> listListList)
        {
            var dataTree = new GH_Structure<GH_Point>();

            for (int i = 0; i < listListList.Count; i++)
            {
                var listList = listListList[i];
                for (int j = 0; j < listList.Count; j++)
                {
                    var list = listList[j];
                    var goos = new List<GH_Point>();
                    for (int k = 0; k < list.Count; k++)
                    {
                        var obj = list[k];
                        GH_Point target = null;
                        GH_Convert.ToGHPoint_Primary(obj, ref target);
                        goos.Add(target);
                    }
                    dataTree.AppendRange(goos, new GH_Path(i, j));
                }
            }

            return dataTree;
        }
    }
}
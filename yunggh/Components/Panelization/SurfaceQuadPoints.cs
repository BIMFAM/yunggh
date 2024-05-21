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
            SortCurvesBySurface.Sort(uCrvs, vCrvs, false, false //flip was messing up the quad corner algorithm, so we'll flip after
                , ref uCrvsSorted
                , ref uIndicesSorted
                , ref vCrvsSorted
                , ref vIndicesSorted);

            //get quad corners
            var quads = GetQuadCorners(uCrvsSorted, vCrvsSorted);

            //flip quad corners
            quads = FlipQuadCorners(quads, uFlip, vFlip);

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

            //get bottom most rows
            GetBottomRow(uCrvs, vCrvs, output);

            //for each quad, find points
            GetInnerQuads(uCrvs, vCrvs, output);

            //get bottom most rows
            GetTopRow(uCrvs, vCrvs, output);

            //get bottom most rows
            GetStartEndColumns(uCrvs, vCrvs, output);

            //find any pentagons by comparing all quad edges with the edges of adjacent quads
            GetPentagons(uCrvs, vCrvs, output);

            //remove lines (when two corners equal the other two corners
            CleanQuads(output);

            return output;
        }

        private static void CleanQuads(List<List<List<Point3d>>> output)
        {
            for (int u = 0; u < output.Count; u++)
            {
                for (int v = 0; v < output[u].Count; v++)
                {
                    var quad = output[u][v];

                    // Count unique points (excluding Point3d.Unset)
                    var withoutUnset = quad.Where(p => p != Point3d.Unset).ToList();
                    var uniquePoints = quad.Where(p => p != Point3d.Unset).Distinct().ToList();

                    if (uniquePoints.Count == 0 || uniquePoints.Count == 2 || withoutUnset.Count != uniquePoints.Count) //no unique points
                    {
                        quad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        output[u][v] = quad;
                        continue; // Move to the next quad
                    }
                    else if (uniquePoints.Count == 3) //triangle quad
                    {
                        // If there are three unique points,
                        // check if two points are the same
                        int unsetCount = uniquePoints.Count(p => p == Point3d.Unset);
                        if (unsetCount == 2)
                        {
                            // If two points are the same, set the quad to Unset
                            quad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                            output[u][v] = quad;
                            continue; // Move to the next quad
                        }
                    }
                    //*/
                    // Check for any combination of points being equal
                    bool[] unsetFlags = new bool[4];
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = i + 1; j < 4; j++)
                        {
                            if (quad[i] != Point3d.Unset && quad[j] != Point3d.Unset && quad[i].DistanceTo(quad[j]) < 0.001)
                            {
                                unsetFlags[i] = true;
                                unsetFlags[j] = true;
                            }
                        }
                    }

                    // Unset flagged points
                    for (int i = 0; i < 4; i++)
                    {
                        if (unsetFlags[i])
                        {
                            quad[i] = Point3d.Unset;
                        }
                    }
                    //*/

                    var unsetPoints = quad.Where(p => p == Point3d.Unset).ToList();
                    if (unsetPoints.Count >= 3)
                    {
                        quad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        output[u][v] = quad;
                        continue; // Move to the next quad
                    }
                    output[u][v] = quad;
                }
            }
        }

        private static void GetPentagons(List<Curve> uCrvs, List<Curve> vCrvs, List<List<List<Point3d>>> output)
        {
            for (int u = 1; u < output.Count - 1; u++) //we can skip the bottommost row, we've already gotten that
            {
                var row = output[u];
                for (int v = 1; v < row.Count; v++)
                {
                    var leftQuad = row[v - 1];
                    var rightQuad = row[v];

                    //if sides already touch
                    if (leftQuad[1] == rightQuad[0] && leftQuad[2] == rightQuad[3]) { continue; }
                    if (leftQuad[1].DistanceTo(rightQuad[0]) < 0.001 && leftQuad[2].DistanceTo(rightQuad[3]) < 0.001) { continue; }
                    //we have to find out which point isn't touching
                    if (leftQuad[1] == rightQuad[0]) //point left 2 and right 3 aren't touching
                    {
                        //find out which point is on a U Crv
                        var leftDist = GetClosestDistance(leftQuad[2], vCrvs);
                        var rightDist = GetClosestDistance(rightQuad[3], vCrvs);
                        if (leftDist < rightDist)
                        {
                            Point3d temp = rightQuad[3];
                            rightQuad[3] = leftQuad[2];
                            rightQuad[4] = temp;
                            row[v] = rightQuad;
                        }
                        else
                        {
                            Point3d temp = leftQuad[2];
                            leftQuad[2] = rightQuad[3];
                            leftQuad[4] = temp;
                            row[v - 1] = leftQuad;
                        }
                    }
                    else //point left 1 and right 0 aren't touching
                    {
                        //find out which point is on a U Crv
                        //find out which point is on a U Crv
                        var leftDist = GetClosestDistance(leftQuad[1], vCrvs);
                        var rightDist = GetClosestDistance(rightQuad[0], vCrvs);
                        if (leftDist < rightDist)
                        {
                            Point3d temp = rightQuad[0];
                            rightQuad[0] = leftQuad[1];
                            rightQuad[4] = temp;
                            row[v] = rightQuad;
                        }
                        else
                        {
                            Point3d temp = leftQuad[1];
                            leftQuad[1] = rightQuad[0];
                            leftQuad[4] = temp;
                            row[v - 1] = leftQuad;
                        }
                    }
                }
                output[u] = row;
            }
        }

        public static double GetClosestDistance(Point3d pt, List<Curve> crvs)
        {
            double minDistance = double.MaxValue;

            foreach (var crv in crvs)
            {
                double t;
                crv.ClosestPoint(pt, out t);
                Point3d cp = crv.PointAt(t);
                double distance = cp.DistanceTo(pt);
                if (distance > minDistance) { continue; }

                minDistance = distance;
            }

            return minDistance;
        }

        private static void GetInnerQuads(List<Curve> uCrvs, List<Curve> vCrvs, List<List<List<Point3d>>> output)
        {
            for (int u = 1; u < output.Count - 1; u++)
            {
                var quadRow = output[u];

                //get first column

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

                //get last column

                output[u] = quadRow;
            }
        }

        private static void GetBottomRow(List<Curve> uCrvs, List<Curve> vCrvs, List<List<List<Point3d>>> output)
        {
            var bottomRow = output[0]; //0 is the bottom row

            for (int v = 1; v < bottomRow.Count - 1; v++)
            {
                var quad = bottomRow[v];
                var topUCrv = uCrvs[0];
                var leftVCrv = vCrvs[v - 1];
                var rightVCrv = vCrvs[v];

                var topLeft = GetIntersection(topUCrv, leftVCrv);
                var topRight = GetIntersection(topUCrv, rightVCrv);

                //here we'll test for the left most column
                if (v == 1 && topLeft != Point3d.Unset)
                {
                    if (topLeft != topUCrv.PointAtStart)
                    {
                        var startQuad = new List<Point3d>
                        {
                            topUCrv.PointAtStart,
                            topLeft,
                            leftVCrv.PointAtStart,
                            Point3d.Unset,
                            Point3d.Unset
                        };
                        bottomRow[0] = startQuad;
                    }
                }

                //if there aren't any intersections then we have to continue
                if (topLeft == Point3d.Unset && topRight == Point3d.Unset) { continue; }

                //not sure why, but we have to test this twice
                if (topLeft == Point3d.Unset)
                {
                    quad[0] = topUCrv.PointAtStart;
                    quad[1] = topRight;
                    quad[2] = rightVCrv.PointAtStart;
                }
                else if (topRight == Point3d.Unset)
                {
                    quad[0] = topLeft;
                    quad[1] = topUCrv.PointAtEnd;
                    quad[3] = leftVCrv.PointAtStart;
                }

                //test if intersection is the same as the VCrv start point (start is towards bottom)
                if (topLeft == leftVCrv.PointAtEnd
                    || topLeft.DistanceTo(leftVCrv.PointAtEnd) < 0.001
                    || topRight == rightVCrv.PointAtEnd
                    || topRight.DistanceTo(rightVCrv.PointAtEnd) < 0.001)
                {
                    continue;
                }

                //check if it's triangle
                if (topLeft == Point3d.Unset)
                {
                    quad[0] = topUCrv.PointAtStart;
                    quad[1] = topRight;
                    quad[2] = rightVCrv.PointAtStart;
                }
                else if (topRight == Point3d.Unset)
                {
                    quad[0] = topLeft;
                    quad[1] = topUCrv.PointAtEnd;
                    quad[3] = leftVCrv.PointAtStart;
                }
                else
                {
                    quad[0] = topLeft;
                    quad[1] = topRight;
                    quad[2] = rightVCrv.PointAtStart;
                    quad[3] = leftVCrv.PointAtStart;
                }
                //*/
                if (quad[0] == leftVCrv.PointAtEnd
                    && quad[1] == rightVCrv.PointAtEnd
                    && quad[2] == rightVCrv.PointAtStart
                    && quad[3] == leftVCrv.PointAtStart) { continue; }
                if (topLeft.DistanceTo(leftVCrv.PointAtEnd) < 0.001) { continue; }
                if (topRight.DistanceTo(rightVCrv.PointAtEnd) < 0.001) { continue; }
                bottomRow[v] = quad;

                //here we test for the right most column
                if (v == bottomRow.Count - 2 && topRight != Point3d.Unset)
                {
                    if (topRight != topUCrv.PointAtEnd)
                    {
                        var endQuad = new List<Point3d>
                        {
                            topRight,
                            topUCrv.PointAtEnd,
                            Point3d.Unset,
                            rightVCrv.PointAtStart,
                            Point3d.Unset
                        };
                        bottomRow[bottomRow.Count - 1] = endQuad;
                    }
                }
                //*/
            }

            output[0] = bottomRow;
        }

        private static void GetTopRow(List<Curve> uCrvs, List<Curve> vCrvs, List<List<List<Point3d>>> output)
        {
            var topRow = output[output.Count - 1]; // Last index is the top row

            for (int v = 1; v < topRow.Count - 1; v++)
            {
                var quad = topRow[v];
                var topUCrv = uCrvs[uCrvs.Count - 1];
                var leftVCrv = vCrvs[v - 1];
                var rightVCrv = vCrvs[v];

                var topLeft = GetIntersection(topUCrv, leftVCrv);
                var topRight = GetIntersection(topUCrv, rightVCrv);

                // Test for the leftmost column
                if (v == 1 && topLeft != Point3d.Unset)
                {
                    if (topLeft != topUCrv.PointAtStart)
                    {
                        var startQuad = new List<Point3d>
                        {
                            topLeft,
                            topUCrv.PointAtStart,
                            leftVCrv.PointAtStart,
                            Point3d.Unset
                        };
                        topRow[0] = startQuad;
                    }
                }

                // If there aren't any intersections then we have to continue
                if (topLeft == Point3d.Unset && topRight == Point3d.Unset) { continue; }

                // Check if it's a triangle
                if (topLeft == Point3d.Unset)
                {
                    quad[1] = topUCrv.PointAtStart;
                    quad[0] = topRight;
                    quad[3] = rightVCrv.PointAtEnd;
                }
                else if (topRight == Point3d.Unset)
                {
                    quad[1] = topLeft;
                    quad[0] = topUCrv.PointAtEnd;
                    quad[2] = leftVCrv.PointAtEnd;
                }

                // Test if intersection is the same as the VCrv start point (start is towards bottom)
                if (topLeft == leftVCrv.PointAtStart || topLeft.DistanceTo(leftVCrv.PointAtStart) < 0.001) { continue; }
                if (topRight == rightVCrv.PointAtStart || topRight.DistanceTo(rightVCrv.PointAtStart) < 0.001) { continue; }

                // Check if it's a triangle
                if (topLeft == Point3d.Unset)
                {
                    quad[1] = topUCrv.PointAtStart;
                    quad[0] = topRight;
                    quad[3] = rightVCrv.PointAtEnd;
                }
                else if (topRight == Point3d.Unset)
                {
                    quad[1] = topLeft;
                    quad[0] = topUCrv.PointAtEnd;
                    quad[2] = leftVCrv.PointAtEnd;
                }
                else
                {
                    quad[1] = topLeft;
                    quad[0] = topRight;
                    quad[3] = rightVCrv.PointAtEnd;
                    quad[2] = leftVCrv.PointAtEnd;
                }
                topRow[v] = quad;

                // Test for the rightmost column
                if (v == topRow.Count - 2 && topRight != Point3d.Unset)
                {
                    if (topRight != topUCrv.PointAtEnd)
                    {
                        var endQuad = new List<Point3d>
                        {
                            topRight,
                            topUCrv.PointAtEnd,
                            Point3d.Unset,
                            rightVCrv.PointAtEnd
                        };
                        topRow[topRow.Count - 1] = endQuad;
                    }
                }
            }
            output[output.Count - 1] = topRow;
        }

        private static void GetStartEndColumns(List<Curve> uCrvs, List<Curve> vCrvs, List<List<List<Point3d>>> output)
        {
            for (int u = 1; u < output.Count - 1; u++) //we can skip the bottommost row, we've already gotten that
            {
                var row = output[u];
                if (row.Count < 3) { continue; }
                var startQuad = row[0];
                var endQuad = row[row.Count - 1];
                var LeftQuad = row[1];
                var RightQuad = row[row.Count - 2];
                var topUCrv = uCrvs[u];
                var botUCrv = uCrvs[u - 1];
                var leftVCrv = vCrvs[0];
                var rightVCrv = vCrvs[vCrvs.Count - 1];
                //quad definition
                //0,1
                //3,2

                //get start quad
                if (LeftQuad[2] != Point3d.Unset && LeftQuad[3] != Point3d.Unset)
                {
                    var topLeft = GetIntersection(leftVCrv, topUCrv);
                    var bottomLeft = GetIntersection(leftVCrv, botUCrv);

                    if (topLeft != Point3d.Unset || bottomLeft != Point3d.Unset)
                    {
                        if (topLeft != startQuad[1] && topLeft != Point3d.Unset)
                        {
                            startQuad[1] = topLeft;
                            startQuad[0] = topUCrv.PointAtStart;
                        }
                        if (bottomLeft != startQuad[2] && bottomLeft != Point3d.Unset)
                        {
                            startQuad[2] = bottomLeft;
                            startQuad[3] = botUCrv.PointAtStart;
                        }

                        if (startQuad[2] == Point3d.Unset) { startQuad[2] = leftVCrv.PointAtStart; }
                        if (startQuad[1] == Point3d.Unset) { startQuad[1] = leftVCrv.PointAtEnd; }

                        //make sure it's not spanning along hte U
                        if (startQuad[0] == topUCrv.PointAtStart
                            && startQuad[1] == topUCrv.PointAtEnd
                            && startQuad[2] == botUCrv.PointAtEnd
                            && startQuad[3] == botUCrv.PointAtStart)
                        {
                            startQuad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        }
                        if (startQuad[0].DistanceTo(topUCrv.PointAtStart) < 0.001
                            && startQuad[1].DistanceTo(topUCrv.PointAtEnd) < 0.001
                            && startQuad[2].DistanceTo(botUCrv.PointAtEnd) < 0.001
                            && startQuad[3].DistanceTo(botUCrv.PointAtStart) < 0.001)
                        {
                            startQuad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        }

                        row[0] = startQuad;
                    }
                }

                //get end quad
                if (RightQuad[2] != Point3d.Unset && RightQuad[3] != Point3d.Unset)
                {
                    var topRight = GetIntersection(rightVCrv, topUCrv);
                    var bottomRight = GetIntersection(rightVCrv, botUCrv);

                    if (topRight != Point3d.Unset || bottomRight != Point3d.Unset)
                    {
                        if (topRight != endQuad[0] && topRight != Point3d.Unset)
                        {
                            endQuad[0] = topRight;
                            endQuad[1] = topUCrv.PointAtEnd;
                        }
                        if (bottomRight != endQuad[3] && bottomRight != Point3d.Unset)
                        {
                            endQuad[3] = bottomRight;
                            endQuad[2] = botUCrv.PointAtEnd;
                        }

                        if (endQuad[3] == Point3d.Unset) { endQuad[2] = rightVCrv.PointAtStart; }
                        if (endQuad[0] == Point3d.Unset) { endQuad[1] = rightVCrv.PointAtEnd; }

                        //make sure it's not spanning along hte U
                        if (endQuad[0] == topUCrv.PointAtStart
                            && endQuad[1] == topUCrv.PointAtEnd
                            && endQuad[2] == botUCrv.PointAtEnd
                            && endQuad[3] == botUCrv.PointAtStart)
                        {
                            endQuad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        }
                        if (endQuad[0].DistanceTo(topUCrv.PointAtStart) < 0.001
                            && endQuad[1].DistanceTo(topUCrv.PointAtEnd) < 0.001
                            && endQuad[2].DistanceTo(botUCrv.PointAtEnd) < 0.001
                            && endQuad[3].DistanceTo(botUCrv.PointAtStart) < 0.001)
                        {
                            endQuad = new List<Point3d>() { Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset, Point3d.Unset };
                        }

                        row[row.Count - 1] = endQuad;
                    }
                }
                output[u] = row;
            }
        }

        public static List<List<List<Point3d>>> FlipQuadCorners(List<List<List<Point3d>>> quads, bool uFlip, bool vFlip)
        {
            // Create a new list to store the flipped quads
            var flippedQuads = new List<List<List<Point3d>>>();

            // If uFlip is true, reverse the outer list
            if (uFlip)
            {
                quads.Reverse();
            }

            // Iterate through each row (U direction)
            foreach (var row in quads)
            {
                // Create a new row to store the flipped columns
                var flippedRow = new List<List<Point3d>>(row);

                // If vFlip is true, reverse the current row
                if (vFlip)
                {
                    flippedRow.Reverse();
                }

                // Add the (potentially flipped) row to the flippedQuads list
                flippedQuads.Add(flippedRow);
            }

            // Return the final flipped quads
            return flippedQuads;
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

            #endregion One Point Missing

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

            #endregion Two Points Missing

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
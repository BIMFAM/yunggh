// Copyright (c) 2022 archgame
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
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    public class ContinuousPanels : GH_Component
    {
        public ContinuousPanels()
          : base("Continuous Panel", "PNL",
              "Panelizes a Polysurface such that panel direction continues across folds.",
              "yung gh", "Panelization")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Facade", "B", "Polysurface to be panelized", GH_ParamAccess.item);
            pManager.AddCurveParameter("Guide Curve", "C", "Guide Curve determining panel start and direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Panel Width", "W", "Panel Width", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Panel Height", "H", "Panel Height", GH_ParamAccess.item, 1);
            pManager.AddBooleanParameter("UV Direction", "D", "Panel UV Direction", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Bond Shift", "S", "Panel Bond Shift Percentage.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Panels", "P", "Panel Breps.", GH_ParamAccess.tree);
            pManager.AddTextParameter("IDs", "ID", "Unique Identifier for each panel", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Debug.WriteLine("Solve Instance Started");
            //get inputs
            Brep facade = null;
            Curve guideCurve = null;
            double panelWidth = 1;
            double panelHeight = 1;
            bool uvDirection = true;
            double bondShift = 0;

            if (!DA.GetData(0, ref facade)) return;
            if (!DA.GetData(1, ref guideCurve)) return;
            if (!DA.GetData(2, ref panelWidth)) return;
            if (!DA.GetData(3, ref panelHeight)) return;
            if (!DA.GetData(4, ref uvDirection)) return;
            if (!DA.GetData(5, ref bondShift)) return;

            //main script
            var panels = new GH_Structure<GH_Rectangle>();
            var ids = new GH_Structure<GH_String>();

            //1) unroll facade
            Point3d[] points;
            Brep[] breps;
            UnrollFacade(facade, guideCurve, out points, out breps);
            Brep unrolledFacade = breps[0];

            //2) create working oriented plane
            Plane plane = GetOrientedPlane(points);

            //3) making a basic panel grid
            int bays, rows;
            var grid = CreateSizedRectangleGrid(panelWidth, panelHeight, unrolledFacade, plane, out bays, out rows);
            int BAYS = bays;
            int ROWS = rows;
            var GRID = grid;

            //4) shift panel grid
            var shifted = ShiftRectangleGridBondPattern(panelWidth, panelHeight, uvDirection, bondShift, plane, ref GRID);
            var SHIFT = shifted;

            //5) move grid so it is over original unroll surface
            List<List<Rectangle3d>> align = AlignRectangleGridWithUnrolledSurface(panelWidth, panelHeight, plane, grid, BAYS, ROWS, GRID, SHIFT);

            //6) remove unused panels for efficiency
            List<Rectangle3d> panelsUsed;
            List<string> idsUsed;
            List<Curve> interiorEdges;
            Curve[] nakedEdges;
            RemoveUnusedPanels(unrolledFacade, align, out panelsUsed, out idsUsed, out interiorEdges, out nakedEdges);

            //7) clip edge panels

            //8) mapping panels to original surface

            //output
            DA.SetDataList(0, panelsUsed);
            DA.SetDataList(1, idsUsed);

            Debug.WriteLine("Solve Instance Ended");
        }

        private static void RemoveUnusedPanels(Brep unrolledFacade, List<List<Rectangle3d>> align, out List<Rectangle3d> output, out List<string> idsUsed, out List<Curve> interiorEdges, out Curve[] joins)
        {
            //6.1) turn unrolled surface into mesh
            var maxMeshEdge = 1000;
            var mesh = BrepToMesh(unrolledFacade, maxMeshEdge);

            //6.2) for each grid panel, test if it is close to the mesh
            double offTol = 1;
            output = new List<Rectangle3d>();
            idsUsed = new List<string>();
            for (int b = 0; b < align.Count; b++)
            {
                //GH_Path path = GRID.Path(b);
                var crvs = align[b]; //branch curves
                var bIds = new List<string>();//branch ids (empty atm)
                for (int i = crvs.Count - 1; i >= 0; i--)//looping backwards
                {
                    var crv = crvs[i];

                    //2.1) test if curve is close to unrolled brep
                    bool inside = false;
                    for (int c = 0; c < 4; c++)
                    {
                        Point3d start = crv.Corner(c);
                        Point3d mCP = mesh.ClosestPoint(start);
                        double dist = start.DistanceTo(mCP);
                        if (dist > offTol) { continue; }//continue means go to next loop iteration/skip
                        inside = true;
                        break;//exit the loop
                    }

                    //2.2) remove curve if outside, else add GUID to output
                    if (inside)
                    {
                        string id = b.ToString() + "-" + i.ToString();// b = branch, i = curve //TODO: panel count matches
                        bIds.Insert(0, id); //add to the beginning of List
                    }
                    else
                    {
                        crvs.RemoveAt(i);//delete object at index from list
                    }
                }
                output.AddRange(crvs);
                idsUsed.AddRange(bIds);
            }

            //6.3) get unrolled facade geometry parts
            List<Curve> nakedEdges = new List<Curve>();
            interiorEdges = new List<Curve>();
            foreach (BrepEdge brepEdge in unrolledFacade.Edges)
            {
                if (brepEdge.Valence == EdgeAdjacency.Interior)
                    interiorEdges.Add(brepEdge.EdgeCurve);
                else if (brepEdge.Valence == EdgeAdjacency.Naked)
                    nakedEdges.Add(brepEdge.EdgeCurve);
            }

            joins = Curve.JoinCurves(nakedEdges, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
        }

        public static Mesh BrepToMesh(Brep brep, double maxEdge)
        {
            //make meshes
            MeshingParameters mp = new MeshingParameters();
            mp.MaximumEdgeLength = maxEdge;
            Mesh[] meshes = Mesh.CreateFromBrep(brep, mp);

            //combine meshes
            Mesh mesh = new Mesh();
            foreach (Mesh m in meshes)
            {
                mesh.Append(m);
            }
            return mesh;
        }

        private static List<List<Rectangle3d>> AlignRectangleGridWithUnrolledSurface(double panelWidth, double panelHeight, Plane plane, List<List<Rectangle3d>> grid, int BAYS, int ROWS, List<List<Rectangle3d>> GRID, List<List<Rectangle3d>> SHIFT)
        {
            //5.1) get move distances
            double X = PanelOffset(BAYS, panelWidth);
            double Y = PanelOffset(ROWS, panelHeight);

            //5.2) create move vector and transform
            Vector3d move = (-X * plane.XAxis) + (-Y * plane.YAxis);
            Transform xform = Transform.Translation(move);
            //Print(move.ToString());

            //5.3) move panels
            var align = new List<List<Rectangle3d>>();
            for (int b = 0; b < GRID.Count; b++)
            {
                //GH_Path path = GRID[b];
                //Print(path.ToString());
                var list = new List<Rectangle3d>();
                var alignedGrid = SHIFT[b];
                for (int i = 0; i < grid.Count; i++)
                {
                    var crv = alignedGrid[i];
                    crv.Transform(xform);
                    alignedGrid[i] = crv;
                }
                //align.AddRange(alignedGrid, path);
                align.Add(alignedGrid);
            }

            return align;
        }

        // <Custom additional code>
        private static double PanelOffset(int count, double panelSize)
        {
            double offset = count / 2.0;
            offset = Math.Round(offset);
            offset *= panelSize;
            return offset;
        }

        private static List<List<Rectangle3d>> ShiftRectangleGridBondPattern(double panelWidth, double panelHeight, bool uvDirection, double bondShift, Plane plane, ref List<List<Rectangle3d>> GRID)
        {
            //4.1) get move vector info
            Vector3d move = plane.YAxis;
            double dim = panelHeight;
            if (uvDirection)
            {
                move = plane.XAxis;
                dim = panelWidth;
                GRID = Transpose(GRID); //Transpose DataTree
            }

            //4.2) shift each row
            var shifted = new List<List<Rectangle3d>>();
            double dist = 0;
            for (int b = 0; b < GRID.Count; b++)
            {
                //Print(dist.ToString());
                //4.2.1) create transform
                Transform xform = Transform.Translation(move * dist);

                //4.2.2) shift grid with transform
                //GH_Path path = GRID.Path(b);
                List<Rectangle3d> rectGrid = GRID[b];
                shifted.Add(new List<Rectangle3d>());
                for (int i = 0; i < rectGrid.Count; i++)
                {
                    Rectangle3d crv = rectGrid[i];
                    crv.Transform(xform);
                    //grid[i] = crv;
                    shifted[b].Add(crv);
                }

                //4.2.3) add shifted curves to datatree output
                //shifted.AddRange(grid, path);

                //4.2.4) update panel move distance
                dist += bondShift * dim;
                if (dist < dim) { continue; }//guard statement
                dist -= dim;
            }

            return shifted;
        }

        public static List<List<T>> Transpose<T>(List<List<T>> data)
        {
            return data.SelectMany(inner => inner.Select((item, index) => new { item, index }))
                        .GroupBy(i => i.index, i => i.item)
                        .Select(g => g.ToList())
                        .ToList();
        }

        private static List<List<Rectangle3d>> CreateSizedRectangleGrid(double panelWidth, double panelHeight, Brep unrolledFacade, Plane plane, out int bays, out int rows)
        {
            //3.1) get bounding box
            Box wbb;
            BoundingBox bb = unrolledFacade.GetBoundingBox(plane, out wbb);
            //GRID = wbb;

            //3.2) get panel counts
            double boxW = wbb.X.Length; //bounding box width
            boxW /= panelWidth; //boxW = boxW/W; //bb width divided panel width
            boxW *= 2; //margin of error for panel shift
            bays = (int)boxW;

            //3.3
            double boxH = wbb.Y.Length;
            boxH /= panelHeight; //boxW = boxW/W;
            boxH *= 2;
            rows = (int)boxH;

            //3.4) create grid
            //Rectangle3d rect = new Rectangle3d(PLN, W, H);
            var grid = new List<List<Rectangle3d>>();
            for (int i = 0; i < bays; i++) //columns
            {
                var list = new List<Rectangle3d>();
                GH_Path path = new GH_Path(i);
                for (int j = 0; j < rows; j++)
                {
                    Rectangle3d rect = new Rectangle3d(plane, panelWidth, panelHeight);
                    Vector3d moveX = plane.XAxis * i * panelWidth;
                    Vector3d moveY = plane.YAxis * j * panelHeight;
                    Transform xform = Transform.Translation(moveX + moveY); //Move Component
                    rect.Transform(xform);

                    //add to dictionary
                    list.Add(rect);
                }
                grid.Add(list);
            }

            return grid;
        }

        private static Plane GetOrientedPlane(Point3d[] points)
        {
            Point3d start = points[0];
            Point3d end = points[1];
            Plane plane = new Plane(Plane.WorldXY);
            plane.Origin = start;

            //align plane
            Vector3d dir = end - start;
            double angle = Vector3d.VectorAngle(Vector3d.XAxis, dir);
            plane.Rotate(angle, Vector3d.ZAxis);
            return plane;
        }

        private static void UnrollFacade(Brep facade, Curve guideCurve, out Point3d[] points, out Brep[] breps)
        {
            //1.1) unroll variables
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Debug.WriteLine(tol.ToString());
            Rhino.Geometry.Unroller unroll = new Rhino.Geometry.Unroller(facade);
            unroll.AbsoluteTolerance = tol;
            unroll.RelativeTolerance = tol;
            unroll.ExplodeOutput = false;

            //1.2) add guide curve as end points to unroll
            Point3d start = guideCurve.PointAtStart;
            Point3d end = guideCurve.PointAtEnd;
            unroll.AddFollowingGeometry(start);
            unroll.AddFollowingGeometry(end);

            //1.3) unroll
            Curve[] curves;
            TextDot[] dots;
            breps = unroll.PerformUnroll(out curves, out points, out dots);
            breps = Brep.JoinBreps(breps, tol);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resource.yunggh;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("3E74927D-C274-46C0-9329-C4ADE467B0FC"); }
        }
    }
}
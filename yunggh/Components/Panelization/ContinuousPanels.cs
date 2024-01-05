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

            //4) shift panel grid
            var shifted = ShiftRectangleGridBondPattern(panelWidth, panelHeight, uvDirection, bondShift, plane, ref grid);

            //5) move grid so it is over original unroll surface
            List<List<Rectangle3d>> align = AlignRectangleGridWithUnrolledSurface(panelWidth, panelHeight, plane, bays, rows, shifted);

            //6) remove unused panels for efficiency
            List<Rectangle3d> panelsUsed;
            List<string> idsUsed;
            List<Curve> interiorEdges;
            Curve[] nakedEdges;
            RemoveUnusedPanels(unrolledFacade, align, out panelsUsed, out idsUsed, out interiorEdges, out nakedEdges);
            Curve En = nakedEdges[0];

            //7) clip edge panels
            var splitPanels = SplitPanels(panelsUsed, interiorEdges, En);
            var idsDict = new Dictionary<string, string>();
            for (int i = 0; i < idsUsed.Count; i++)
                idsDict.Add((new GH_Path(i)).ToString(), idsUsed[i]);

            //8) mapping panels to original surface
            Dictionary<GH_Path, List<Curve>> panelsMapped;
            Dictionary<GH_Path, List<string>> idsMapped;
            MapPanels(facade, unrolledFacade, splitPanels, idsDict, out panelsMapped, out idsMapped);

            //9) organize output by row
            if (panelDataTreebyRow)
            {
                //9.1) sort curves into rows
                var panelByRow = new Dictionary<string, List<Curve>>();
                var idsByRow = new Dictionary<string, List<string>>();
                for (int i = 0; i < panelsMapped.Count; i++)
                {
                    var panels = panelsMapped.ElementAt(i).Value;
                    var ids = idsMapped.ElementAt(i).Value;

                    for (int j = 0; j < panels.Count; j++) //looping per each facade face
                    {
                        var panel = panels[j];
                        var id = ids[j];
                        var row = id.Split('-')[0];

                        if (!panelByRow.ContainsKey(row))
                        {
                            panelByRow.Add(row, new List<Curve>());
                            idsByRow.Add(row, new List<string>());
                        }
                        panelByRow[row].Add(panel);
                        idsByRow[row].Add(id);
                    }
                }

                //9.2) translate dictionary into DataTree
                panelsMapped = new Dictionary<GH_Path, List<Curve>>();
                idsMapped = new Dictionary<GH_Path, List<string>>();
                for (int i = 0; i < panelByRow.Count; i++)
                {
                    //9.2.1) get inputs
                    var row = panelByRow.ElementAt(i).Key;
                    var panels = panelByRow.ElementAt(i).Value;
                    var ids = idsByRow.ElementAt(i).Value;

                    //9.2.2) sort panels by id
                    panels = panels.Select((n, index) => new { Name = n, Index = index })
                                    .OrderBy(x => ids.ElementAtOrDefault(x.Index))
                                    .Select(x => x.Name)
                                    .ToList();
                    ids.Sort();

                    //9.2.3) add to datatree
                    int rowInt;
                    if (!int.TryParse(row, out rowInt)) { continue; }
                    GH_Path path = new GH_Path(rowInt);
                    panelsMapped.Add(path, panels);
                    idsMapped.Add(path, ids);
                }
            }

            //output
            var outputPanels = DictionaryToGHStructure(panelsMapped);
            var outputIds = DictionaryToGHStructure(idsMapped);
            DA.SetDataTree(0, outputPanels);
            DA.SetDataTree(1, outputIds);

            Debug.WriteLine("Solve Instance Ended");
        }

        public static GH_Structure<GH_Curve> DictionaryToGHStructure(Dictionary<GH_Path, List<Curve>> dict)
        {
            var tree = new GH_Structure<GH_Curve>();
            foreach (var kvp in dict)
            {
                var crvs = new List<GH_Curve>();
                foreach (var crv in kvp.Value)
                {
                    GH_Curve ghCrv = null;
                    if (!GH_Convert.ToGHCurve(crv, GH_Conversion.Primary, ref ghCrv)) { continue; }
                    crvs.Add(ghCrv);
                }
                tree.AppendRange(crvs, kvp.Key);
            }
            return tree;
        }

        public static GH_Structure<GH_String> DictionaryToGHStructure(Dictionary<GH_Path, List<string>> dict)
        {
            var tree = new GH_Structure<GH_String>();
            foreach (var kvp in dict)
            {
                var crvs = new List<GH_String>();
                foreach (var crv in kvp.Value)
                {
                    GH_String ghCrv = null;
                    if (!GH_Convert.ToGHString(crv, GH_Conversion.Primary, ref ghCrv)) { continue; }
                    crvs.Add(ghCrv);
                }
                tree.AppendRange(crvs, kvp.Key);
            }
            return tree;
        }

        private static void MapPanels(Brep facade, Brep unrolledFacade, Dictionary<string, List<Curve>> splitPanels, Dictionary<string, string> idsDict, out Dictionary<GH_Path, List<Curve>> panelsMapped, out Dictionary<GH_Path, List<string>> idsMapped)
        {
            //8.1)get facades and unrolls as faces and create data tree paths
            Dictionary<int, GH_Path> paths = new Dictionary<int, GH_Path>();
            List<Brep> unrolls = new List<Brep>(); //unrolled flat faces
            List<Brep> facades = new List<Brep>(); //original facade faces
            for (int i = 0; i < facade.Faces.Count; i++)
            {
                GH_Path path = new GH_Path(i);
                paths.Add(i, path);
                facades.Add(facade.Faces[i].DuplicateFace(false));
                unrolls.Add(unrolledFacade.Faces[i].DuplicateFace(false));
            }

            //8.2)map panels to faces
            panelsMapped = new Dictionary<GH_Path, List<Curve>>();
            idsMapped = new Dictionary<GH_Path, List<string>>();
            var keys = splitPanels.Keys.ToList();
            for (int b = 0; b < keys.Count; b++)
            {
                var path = keys[b];
                if (!idsDict.ContainsKey(path) || !splitPanels.ContainsKey(path)) { continue; }
                string id = idsDict[keys[b]];
                List<Curve> pnls = splitPanels[keys[b]];
                foreach (Curve pnl in pnls)
                {
                    //8.2.1)find closest facade face index
                    Point3d center = CurveCenter(pnl); //need center for facade face location
                    int facadeIndex = -1;
                    for (int i = 0; i < unrolls.Count; i++)
                    {
                        Brep brep = unrolls[i];
                        Point3d cp = brep.ClosestPoint(center);
                        if (cp.DistanceTo(center) > 0.01) { continue; }//skip if not on unrolled facade face
                        facadeIndex = i; break; //once index is found, leave loop
                    }
                    if (facadeIndex == -1) { continue; }//skip panels not even on the unrolled facade

                    //8.2.2) match UV coordinates across surfaces
                    List<Point3d> pts = CurvePoints(pnl); //need points for UV transfer
                    Surface facadeFace = facades[facadeIndex].Surfaces[0];
                    Surface unrollFace = unrolls[facadeIndex].Surfaces[0];
                    Polyline ply = new Polyline();
                    for (int j = 0; j < pts.Count; j++)
                    {
                        double u;
                        double v;
                        unrollFace.ClosestPoint(pts[j], out u, out v);
                        ply.Add(facadeFace.PointAt(u, v));
                    }
                    ply.Add(ply[0]);

                    if (!panelsMapped.ContainsKey(paths[facadeIndex]))
                    {
                        panelsMapped.Add(paths[facadeIndex], new List<Curve>());
                        idsMapped.Add(paths[facadeIndex], new List<string>());
                    }
                    panelsMapped[paths[facadeIndex]].Add(ply.ToNurbsCurve());
                    idsMapped[paths[facadeIndex]].Add(id);
                }
            }
        }

        public static List<Point3d> CurvePoints(Curve crv)
        {
            List<Point3d> pts = new List<Point3d>();
            Curve[] segs = crv.DuplicateSegments();
            foreach (Curve seg in segs)
            {
                pts.Add(seg.PointAtStart);
            }
            return pts;
        }

        private static Point3d CurveCenter(Curve crv)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Polyline ply = crv.ToPolyline(tol, tol, 0, 10000000).ToPolyline();
            return ply.CenterPoint();
        }

        private static Dictionary<string, List<Curve>> SplitPanels(List<Rectangle3d> panelsUsed, List<Curve> interiorEdges, Curve En)
        {
            //7.1) Split Panels on Edge
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var splitPanels = new Dictionary<string, List<Curve>>();
            for (int i = 0; i < panelsUsed.Count; i++)
            {
                string path = (new GH_Path(i)).ToString();
                List<Curve> crvs = new List<Curve>();
                var crv = panelsUsed[i].ToNurbsCurve().DuplicateCurve(); //one panel curve
                bool onEdge = IsCurveOnCurve(crv, En);
                if (onEdge)
                {
                    Curve[] intersections = Curve.CreateBooleanIntersection(crv, En, tol);
                    foreach (Curve region in intersections)
                    {
                        crvs.Add(region);
                    }
                }
                else
                {
                    crvs.Add(crv);
                }

                //7.2) Split curves that are on a Bend
                List<Curve> crvs2 = new List<Curve>();
                foreach (Curve curve in crvs)
                {
                    List<Curve> splits = SplitCurveWithCurve(curve, interiorEdges);
                    crvs2.AddRange(splits);
                }

                if (!splitPanels.ContainsKey(path))
                {
                    splitPanels.Add(path, new List<Curve>());
                }
                splitPanels[path].AddRange(crvs2);
            }

            return splitPanels;
        }

        private static List<Curve> SplitCurveWithCurve(Curve crv, List<Curve> splitters)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Brep pnl = Brep.CreatePlanarBreps(crv, tol)[0];
            pnl = pnl.Faces[0].Split(splitters, tol);

            //get split panels as curves
            List<Curve> output = new List<Curve>();
            foreach (BrepFace face in pnl.Faces)
            {
                Curve[] edges = face.DuplicateFace(false).DuplicateEdgeCurves();
                Curve edge = Curve.JoinCurves(edges, tol)[0];
                output.Add(edge);
            }

            return output;
        }

        public static bool IsCurveOnCurve(Curve crv, Curve testEdge)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            var events = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv, testEdge, tol, tol);
            if (events.Count > 0) { return true; } //return = "exit function"
            return false;
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

        private static List<List<Rectangle3d>> AlignRectangleGridWithUnrolledSurface(double panelWidth, double panelHeight, Plane plane, int BAYS, int ROWS, List<List<Rectangle3d>> SHIFT)
        {
            //5.1) get move distances
            double X = PanelOffset(BAYS, panelWidth);
            double Y = PanelOffset(ROWS, panelHeight);

            //5.2) create move vector and transform
            Vector3d move = (-X * plane.XAxis) + (-Y * plane.YAxis);
            Transform xform = Transform.Translation(move);

            //5.3) move panels
            var align = new List<List<Rectangle3d>>();
            for (int b = 0; b < SHIFT.Count; b++)
            {
                var alignedGrid = SHIFT[b];
                for (int i = 0; i < alignedGrid.Count; i++)
                {
                    var crv = alignedGrid[i];
                    crv.Transform(xform);
                    alignedGrid[i] = crv;
                }
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

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Panel DataTree by Row", Menu_ClickPanelDataTreeRow);
        }

        private void Menu_ClickPanelDataTreeRow(object sender, EventArgs e)
        {
            panelDataTreebyRow = !panelDataTreebyRow;
            this.ExpireSolution(true);
        }

        public bool panelDataTreebyRow = false;
    }
}
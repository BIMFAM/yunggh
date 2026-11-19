using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace yunggh.Components.Panelization
{
    /// <summary>
    /// Checkerboard pyramids: cells with (row + col) even point up, odd point down.
    /// The up and down pyramid sides that touch on a grid edge are snapped into one plane
    /// through that edge (rotated about it to best fit both tips). Each cell's side on that
    /// plane is clipped by the cell's other edge planes, so the tips become ridges, and the
    /// two coplanar sides are merged into one planar panel per grid edge.
    /// </summary>
    public class CheckerPyramidPanelization : PanelizationBase
    {
        #region UI

        public CheckerPyramidPanelization()
          : base("Checker Pyramid Panelization", "CHKPYRPNL",
                "Panelize Surface with checkerboard up/down pyramids whose touching sides are snapped into shared planes. " +
                "Output: {0;r;c} panels on U direction edges, {1;r;c} panels on V direction edges, {2;r;c} flat panels.")
        {
        }

        protected override System.Drawing.Bitmap Icon
        { get { return Resource.yunggh; } }

        public override Guid ComponentGuid
        { get { return new Guid("A25A7426-24FF-400F-AB2E-E361FED9B46D"); } }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddNumberParameter("Height", "H", "Pyramid Height (negative swaps which checker cells go up)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Truncation", "T", "Normalized Pyramid Truncation Location (1 = full pyramid, 0 = flat)", GH_ParamAccess.item, 1.0);
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        #endregion UI

        private GH_Structure<GH_Brep> _tree;
        public override List<List<List<Brep>>> GetPanels(List<List<List<Point3d>>> quads, Brep brep, IGH_DataAccess DA)
        {
            var height = 1.0;
            var truncation = 1.0;
            DA.GetData(5, ref height);
            DA.GetData(6, ref truncation);

            int fallbackCount;
            _tree = GetCheckerPanels(quads, brep, height, truncation, out fallbackCount);
            if (fallbackCount > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    fallbackCount + " cell(s) couldn't be snapped to the shared planes (pyramid height too small for the cell's size, warp or curvature) and use plain pyramid sides instead. Increase Height to reduce this.");
            }

            //panels are per grid edge, not per cell, so the tree is built directly
            return new List<List<List<Brep>>>();
        }

        public override GH_Structure<GH_Brep> ToDataTree(List<List<List<Brep>>> panels)
        {
            return _tree ?? new GH_Structure<GH_Brep>();
        }

        #region GEOMETRY

        private class Cell
        {
            public int R, C;
            public Point3d[] Corners;     //cyclic: top left, top right, bottom right, bottom left
            public Point3d Center;
            public Point3d Inside;        //a point strictly inside the pyramid
            public Vector3d Dir;          //unit direction from center to tip
            public Point3d Tip;
            public Edge[] Edges = new Edge[4];
            public List<Point3d>[] Faces = new List<Point3d>[4];
            public bool Fallback;         //plain pyramid, sides not on the shared planes
        }

        private class Owner
        {
            public Cell Cell;
            public int Index;             //edge i runs Corners[i] -> Corners[i + 1]
        }

        private class Edge
        {
            public Point3d P0, P1;
            public List<Owner> Owners = new List<Owner>();
            public Plane Plane = Plane.Unset;
        }

        public static GH_Structure<GH_Brep> GetCheckerPanels(List<List<List<Point3d>>> quads, Brep brep, double height, double truncation, out int fallbackCount)
        {
            fallbackCount = 0;
            double tol = SurfaceGrid.DocTolerance();
            var tree = new GH_Structure<GH_Brep>();
            if (quads == null || brep == null || brep.Faces.Count == 0) { return tree; }

            bool flat = truncation <= 0 || Math.Abs(height) < tol;

            //collect cells
            var cells = new List<Cell>();
            for (int r = 0; r < quads.Count; r++)
            {
                for (int c = 0; c < quads[r].Count; c++)
                {
                    var q = quads[r][c];
                    var valid = q.Where(p => p.IsValid).ToList();
                    if (valid.Count < 3) { continue; }

                    bool isQuad = q.Count >= 4 && q.Take(4).All(p => p.IsValid) && (q.Count < 5 || !q[4].IsValid);
                    if (!isQuad || flat)
                    {
                        var pts = isQuad ? q.Take(4).ToList() : OrderByAngle(valid);
                        AddBreps(tree, PyramidPanelization.GetFlatPanel(pts, tol), new GH_Path(2, r, c));
                        continue;
                    }

                    var cell = new Cell { R = r, C = c, Corners = q.Take(4).ToArray() };
                    cell.Center = (cell.Corners[0] + cell.Corners[1] + cell.Corners[2] + cell.Corners[3]) / 4.0;
                    var normal = GetNormal(brep, cell.Corners, cell.Center);
                    double sign = (r + c) % 2 == 0 ? 1.0 : -1.0;
                    var tipVec = normal * height * sign;
                    cell.Tip = cell.Center + tipVec;
                    cell.Dir = tipVec;
                    cell.Dir.Unitize();
                    cell.Inside = cell.Center + tipVec * 0.01;
                    cells.Add(cell);
                }
            }
            if (cells.Count == 0) { return tree; }

            //match shared edges by their corner points
            var verts = new List<Point3d>();
            var rtree = new RTree();
            Func<Point3d, int> vertexId = p =>
            {
                int found = -1;
                rtree.Search(new Sphere(p, tol * 10), (s, e) => { found = e.Id; e.Cancel = true; });
                if (found >= 0) { return found; }
                verts.Add(p);
                rtree.Insert(p, verts.Count - 1);
                return verts.Count - 1;
            };

            var edges = new Dictionary<Tuple<int, int>, Edge>();
            var edgeOrder = new List<Edge>();
            foreach (var cell in cells)
            {
                for (int i = 0; i < 4; i++)
                {
                    var p0 = cell.Corners[i];
                    var p1 = cell.Corners[(i + 1) % 4];
                    int a = vertexId(p0);
                    int b = vertexId(p1);
                    var key = Tuple.Create(Math.Min(a, b), Math.Max(a, b));
                    Edge edge;
                    if (!edges.TryGetValue(key, out edge))
                    {
                        edge = new Edge { P0 = p0, P1 = p1 };
                        edges[key] = edge;
                        edgeOrder.Add(edge);
                    }
                    edge.Owners.Add(new Owner { Cell = cell, Index = i });
                    cell.Edges[i] = edge;
                }
            }

            //edge planes
            foreach (var edge in edgeOrder)
            {
                edge.Plane = GetEdgePlane(edge);
            }

            //each cell's sides: the 4 edge planes meet in a ridge, so the sides are built
            //from the corners and the two ridge points. Neighboring sides share those exact
            //points, so the panels close up with no gaps
            fallbackCount = 0;
            foreach (var cell in cells)
            {
                if (!BuildRidgeFaces(cell, height))
                {
                    //the planes don't make a proper ridge (too shallow for the cell's size,
                    //warp or curvature): use a plain pyramid to the tip, which still closes
                    fallbackCount++;
                    cell.Fallback = true;
                    for (int i = 0; i < 4; i++)
                    {
                        cell.Faces[i] = new List<Point3d> { cell.Corners[i], cell.Corners[(i + 1) % 4], cell.Tip };
                    }
                }

                //truncation: cut with a plane normal to the surface at the pyramid center,
                //at the ratio of the farthest panel point
                if (truncation < 1)
                {
                    double h = 0;
                    foreach (var f in cell.Faces.Where(f => f != null))
                    {
                        foreach (var p in f) { h = Math.Max(h, (p - cell.Center) * cell.Dir); }
                    }
                    if (h > tol)
                    {
                        var cut = new Plane(cell.Center + cell.Dir * (truncation * h), cell.Dir);
                        for (int i = 0; i < 4; i++)
                        {
                            if (cell.Faces[i] == null) { continue; }
                            var clipped = Clip(cell.Faces[i], cut, cell.Center);
                            cell.Faces[i] = clipped.Count >= 3 ? clipped : null;
                        }
                    }
                }
            }

            //merge the coplanar sides of each edge into one panel
            foreach (var edge in edgeOrder)
            {
                var first = edge.Owners[0];
                var path = new GH_Path(first.Index % 2, first.Cell.R, first.Cell.C);

                var second = edge.Owners.Count > 1 ? edge.Owners[1] : null;
                var sideA = SidePath(first.Cell.Faces[first.Index], edge.P0, edge.P1);
                var sideB = second != null ? SidePath(second.Cell.Faces[second.Index], edge.P1, edge.P0) : null;

                bool coplanar = !first.Cell.Fallback && (second == null || !second.Cell.Fallback);
                var halves = new List<Brep>();
                foreach (var side in new[] { sideA, sideB })
                {
                    if (side == null) { continue; }
                    var half = PlanarPanel(side, coplanar ? edge.Plane : Plane.Unset, tol);
                    if (half != null) { halves.Add(half); }
                }

                if (coplanar && sideA != null && sideB != null && halves.Count == 2)
                {
                    //one panel from both sides. In a skewed cell the two sides can fold back
                    //over each other, so the merged outline crosses itself: keep the sides
                    //separate unless the merged panel covers exactly both of them
                    var outline = new List<Point3d>(sideA);
                    if (sideB.Count > 2) { outline.AddRange(sideB.Skip(1).Take(sideB.Count - 2)); }
                    var merged = PlanarPanel(outline, edge.Plane, tol);
                    if (merged != null)
                    {
                        double mergedArea = merged.GetArea();
                        double halvesArea = halves.Sum(h => h.GetArea());
                        if (Math.Abs(mergedArea - halvesArea) <= 1e-3 * halvesArea)
                        {
                            halves = new List<Brep> { merged };
                        }
                    }
                }
                AddBreps(tree, halves, path);
            }

            return tree;
        }

        /// <summary>
        /// The 4 edge planes of a cell meet in a ridge along two opposite planes, either
        /// P0/P2 or P1/P3. With the ridge on Pa/Pa+2 and ridge ends
        /// R1 = Pa∩Pa+1∩Pa+2 and R3 = Pa∩Pa+2∩Pa+3, the sides are:
        /// a: [ca, ca+1, R1, R3], a+1: [ca+1, ca+2, R1], a+2: [ca+2, ca+3, R3, R1], a+3: [ca+3, ca, R3].
        /// Each side starts with its own edge (corner i, corner i+1).
        /// Returns false when neither ridge makes a proper (convex, nearby) top.
        /// </summary>
        private static bool BuildRidgeFaces(Cell cell, double height)
        {
            var planes = cell.Edges.Select(e => e.Plane).ToArray();
            if (planes.Any(p => !p.IsValid)) { return false; }

            double radius = Math.Max(cell.Corners[0].DistanceTo(cell.Corners[2]), cell.Corners[1].DistanceTo(cell.Corners[3])) / 2.0;
            double maxDist = 3.0 * (radius + Math.Abs(height));
            double eps = 1e-6 * (radius + Math.Abs(height));

            List<Point3d>[] best = null;
            double bestRidge = double.MaxValue;
            for (int a = 0; a < 2; a++)
            {
                var pa = planes[a];
                var pb = planes[(a + 1) % 4];
                var pc = planes[(a + 2) % 4];
                var pd = planes[(a + 3) % 4];
                Point3d r1, r3;
                if (!Rhino.Geometry.Intersect.Intersection.PlanePlanePlane(pa, pb, pc, out r1)) { continue; }
                if (!Rhino.Geometry.Intersect.Intersection.PlanePlanePlane(pa, pc, pd, out r3)) { continue; }

                //convex: each ridge end is inside the plane it isn't on
                if (InsideSide(pd, r1, cell.Inside) < -eps) { continue; }
                if (InsideSide(pb, r3, cell.Inside) < -eps) { continue; }

                //on the tip side and not running off
                if ((r1 - cell.Center) * cell.Dir <= 0 || (r3 - cell.Center) * cell.Dir <= 0) { continue; }
                if (r1.DistanceTo(cell.Center) > maxDist || r3.DistanceTo(cell.Center) > maxDist) { continue; }

                //over the cell's footprint, otherwise the sides overhang and fold over
                if (!OverFootprint(cell, r1, eps) || !OverFootprint(cell, r3, eps)) { continue; }

                double ridge = r1.DistanceTo(r3);
                if (ridge >= bestRidge) { continue; }
                bestRidge = ridge;

                Func<int, Point3d> c = k => cell.Corners[k % 4];
                best = new List<Point3d>[4];
                best[a] = new List<Point3d> { c(a), c(a + 1), r1, r3 };
                best[(a + 1) % 4] = new List<Point3d> { c(a + 1), c(a + 2), r1 };
                best[(a + 2) % 4] = new List<Point3d> { c(a + 2), c(a + 3), r3, r1 };
                best[(a + 3) % 4] = new List<Point3d> { c(a + 3), c(a + 4), r3 };
            }

            if (best == null) { return false; }
            cell.Faces = best;
            return true;
        }

        /// <summary>
        /// Is the point inside the prism made by the cell's edges swept along the pyramid direction?
        /// </summary>
        private static bool OverFootprint(Cell cell, Point3d pt, double eps)
        {
            for (int j = 0; j < 4; j++)
            {
                var a = cell.Corners[j];
                var side = Vector3d.CrossProduct(cell.Corners[(j + 1) % 4] - a, cell.Dir);
                if (!side.Unitize()) { continue; }
                if (InsideSide(new Plane(a, side), pt, cell.Center) < -eps) { return false; }
            }
            return true;
        }

        /// <summary>Signed distance of a point to a plane, positive on the side of the inside point.</summary>
        private static double InsideSide(Plane plane, Point3d point, Point3d inside)
        {
            return plane.DistanceTo(point) * Math.Sign(plane.DistanceTo(inside));
        }

        /// <summary>
        /// Plane through the edge, rotated about it to best fit the tips of the cells on
        /// either side. A boundary edge (one cell) is the plain pyramid side.
        /// </summary>
        private static Plane GetEdgePlane(Edge edge)
        {
            var d = edge.P1 - edge.P0;
            if (!d.Unitize()) { return Plane.Unset; }

            //tip directions perpendicular to the edge. They are unitized so the fit bisects
            //the two tip directions: not unitized, a big cell's far tip would dominate the fit
            //and lay the plane nearly flat on the surface, so the small cell's side leans
            //outward and never closes
            var ws = edge.Owners.Select(o =>
            {
                var w = o.Cell.Tip - edge.P0;
                w -= d * (w * d);
                w.Unitize();
                return w;
            }).ToList();

            Vector3d normal;
            if (ws.Count == 1)
            {
                normal = Vector3d.CrossProduct(d, ws[0]);
            }
            else
            {
                //2D basis perpendicular to the edge
                var x = ws[0];
                if (!x.Unitize())
                {
                    x = ws[1];
                    if (!x.Unitize()) { return Plane.Unset; }
                }
                var y = Vector3d.CrossProduct(d, x);
                y.Unitize();

                //normal minimizing the sum of squared tip distances:
                //smallest eigenvector of the 2x2 scatter matrix [[a,b],[b,c]]
                double a = 0, b = 0, c = 0;
                foreach (var w in ws)
                {
                    double wx = w * x, wy = w * y;
                    a += wx * wx; b += wx * wy; c += wy * wy;
                }
                double lambda = (a + c) / 2.0 - Math.Sqrt(((a - c) / 2.0) * ((a - c) / 2.0) + b * b);
                double nx, ny;
                if (Math.Abs(b) > 1e-12) { nx = b; ny = lambda - a; }
                else if (a <= c) { nx = 1; ny = 0; }
                else { nx = 0; ny = 1; }
                normal = x * nx + y * ny;
            }
            if (!normal.Unitize()) { return Plane.Unset; }
            return new Plane(edge.P0, normal);
        }

        /// <summary>Normal of the quad, oriented to agree with the surface.</summary>
        private static Vector3d GetNormal(Brep brep, Point3d[] corners, Point3d center)
        {
            var normal = Vector3d.CrossProduct(corners[2] - corners[0], corners[3] - corners[1]);
            normal.Unitize();

            Point3d cp;
            ComponentIndex ci;
            double s, t;
            Vector3d brepNormal;
            if (brep.ClosestPoint(center, out cp, out ci, out s, out t, 0, out brepNormal))
            {
                if (ci.ComponentIndexType == ComponentIndexType.BrepFace)
                {
                    var face = brep.Faces[ci.Index];
                    brepNormal = face.NormalAt(s, t);
                    if (face.OrientationIsReversed) { brepNormal.Reverse(); }
                }
                if (normal * brepNormal < 0) { normal.Reverse(); }
            }
            return normal;
        }

        /// <summary>Clip a convex polygon by a plane, keeping the side the inside point is on.</summary>
        private static List<Point3d> Clip(List<Point3d> poly, Plane plane, Point3d inside)
        {
            double sIn = plane.DistanceTo(inside);
            if (Math.Abs(sIn) < 1e-12) { return poly; }
            double sg = Math.Sign(sIn);

            var result = new List<Point3d>();
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var cur = poly[i];
                var nxt = poly[(i + 1) % n];
                double dc = sg * plane.DistanceTo(cur);
                double dn = sg * plane.DistanceTo(nxt);
                if (dc >= 0) { result.Add(cur); }
                if ((dc >= 0) != (dn >= 0))
                {
                    double t = dc / (dc - dn);
                    result.Add(cur + (nxt - cur) * t);
                }
            }
            return result;
        }

        /// <summary>
        /// The side polygon's vertices from a to b, going the way that doesn't run along the
        /// base edge. The ends are snapped to a and b exactly so both sides share them.
        /// </summary>
        private static List<Point3d> SidePath(List<Point3d> poly, Point3d a, Point3d b)
        {
            if (poly == null || poly.Count < 3) { return null; }
            int n = poly.Count;
            int ia = ClosestIndex(poly, a);
            int ib = ClosestIndex(poly, b);
            if (ia == ib) { return null; }

            var forward = new List<Point3d>();
            for (int i = ia; ; i = (i + 1) % n) { forward.Add(poly[i]); if (i == ib) { break; } }
            var backward = new List<Point3d>();
            for (int i = ia; ; i = (i - 1 + n) % n) { backward.Add(poly[i]); if (i == ib) { break; } }

            var path = forward.Count >= backward.Count ? forward : backward;
            path[0] = a;
            path[path.Count - 1] = b;
            return path;
        }

        private static int ClosestIndex(List<Point3d> pts, Point3d p)
        {
            int best = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                double dist = pts[i].DistanceToSquared(p);
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }

        private static Brep PlanarPanel(List<Point3d> outline, Plane plane, double tol)
        {
            //remove duplicate consecutive points
            var pts = new List<Point3d>();
            foreach (var p in outline)
            {
                if (pts.Count > 0 && pts[pts.Count - 1].DistanceTo(p) < tol) { continue; }
                pts.Add(p);
            }
            if (pts.Count > 1 && pts[0].DistanceTo(pts[pts.Count - 1]) < tol) { pts.RemoveAt(pts.Count - 1); }
            if (pts.Count < 3) { return null; }

            //project onto the plane to remove numerical noise
            if (plane.IsValid) { pts = pts.Select(p => plane.ClosestPoint(p)).ToList(); }
            pts.Add(pts[0]);

            var breps = Brep.CreatePlanarBreps(new PolylineCurve(pts), tol);
            return breps != null && breps.Length > 0 ? breps[0] : null;
        }

        private static List<Point3d> OrderByAngle(List<Point3d> pts)
        {
            Plane plane;
            if (Plane.FitPlaneToPoints(pts, out plane) != PlaneFitResult.Success) { return pts; }
            var center = new Point3d(pts.Average(p => p.X), pts.Average(p => p.Y), pts.Average(p => p.Z));
            plane.Origin = center;
            return pts.OrderBy(p =>
            {
                double u, v;
                plane.ClosestParameter(p, out u, out v);
                return Math.Atan2(v, u);
            }).ToList();
        }

        private static void AddBreps(GH_Structure<GH_Brep> tree, IEnumerable<Brep> breps, GH_Path path)
        {
            foreach (var b in breps)
            {
                GH_Brep gh = null;
                if (!GH_Convert.ToGHBrep_Primary(b, ref gh)) { continue; }
                tree.Append(gh, path);
            }
        }

        #endregion GEOMETRY
    }
}

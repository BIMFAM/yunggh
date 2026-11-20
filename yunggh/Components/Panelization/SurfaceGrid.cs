using System;
using System.Collections.Generic;
using System.Linq;

using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace yunggh.Components.Panelization
{
    /// <summary>
    /// Builds the panel grid from 'U' and 'V' curves that may be broken into several pieces
    /// (e.g. isocurves of a surface with holes or trims).
    ///
    /// Each curve piece is given the index of the grid line it belongs to by walking the
    /// intersection graph: two U pieces that are consecutive along a V piece sit on
    /// consecutive U lines (and vice versa). Cells are then taken from the surface split by
    /// all of the curves, so they follow holes and trims exactly instead of guessing
    /// missing corners from curve end points.
    /// </summary>
    public class SurfaceGrid
    {
        public class Node
        {
            public int U;       //index of U curve piece
            public int V;       //index of V curve piece
            public Point3d Point;
            public double TU;   //parameter on U piece (multiplied by its orientation sign)
            public double TV;   //parameter on V piece (multiplied by its orientation sign)
        }

        public List<Curve> UCurves { get; private set; }
        public List<Curve> VCurves { get; private set; }
        public List<Node> Nodes { get; private set; }

        /// <summary>Grid line index (row) for each U piece, -1 if unknown.</summary>
        public int[] URow { get; private set; }

        /// <summary>Grid line index (column) for each V piece, -1 if unknown.</summary>
        public int[] VCol { get; private set; }

        /// <summary>+1 or -1 so all pieces of one direction run the same way.</summary>
        public int[] USign { get; private set; }

        public int[] VSign { get; private set; }

        public int RowLineCount { get; private set; }
        public int ColLineCount { get; private set; }

        private readonly double _tol;

        private SurfaceGrid(List<Curve> uCrvs, List<Curve> vCrvs, double tol)
        {
            UCurves = uCrvs.Where(c => c != null && c.IsValid).ToList();
            VCurves = vCrvs.Where(c => c != null && c.IsValid).ToList();
            _tol = tol;
        }

        public static double DocTolerance()
        {
            return Rhino.RhinoDoc.ActiveDoc != null ? Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance : 0.001;
        }

        public static SurfaceGrid Build(List<Curve> uCrvs, List<Curve> vCrvs)
        {
            var grid = new SurfaceGrid(uCrvs, vCrvs, DocTolerance());
            grid.FindNodes();
            grid.Orient();
            grid.AssignLines();
            return grid;
        }

        #region GRAPH

        private void FindNodes()
        {
            Nodes = new List<Node>();
            for (int i = 0; i < UCurves.Count; i++)
            {
                for (int j = 0; j < VCurves.Count; j++)
                {
                    var xs = Intersection.CurveCurve(UCurves[i], VCurves[j], _tol, _tol);
                    if (xs == null) { continue; }
                    foreach (var x in xs)
                    {
                        if (x.IsOverlap) { continue; }
                        Nodes.Add(new Node { U = i, V = j, Point = x.PointA, TU = x.ParameterA, TV = x.ParameterB });
                    }
                }
            }
        }

        private List<Node>[] NodesByU()
        {
            var lists = Enumerable.Range(0, UCurves.Count).Select(_ => new List<Node>()).ToArray();
            foreach (var n in Nodes) { lists[n.U].Add(n); }
            for (int i = 0; i < lists.Length; i++) { lists[i] = lists[i].OrderBy(n => n.TU).ToList(); }
            return lists;
        }

        private List<Node>[] NodesByV()
        {
            var lists = Enumerable.Range(0, VCurves.Count).Select(_ => new List<Node>()).ToArray();
            foreach (var n in Nodes) { lists[n.V].Add(n); }
            for (int j = 0; j < lists.Length; j++) { lists[j] = lists[j].OrderBy(n => n.TV).ToList(); }
            return lists;
        }

        /// <summary>
        /// Make every piece of one direction run the same way: pieces that are neighbors
        /// along a crossing curve should be roughly parallel.
        /// </summary>
        private void Orient()
        {
            USign = OrientFamily(UCurves, NodesByV(), n => n.U, (n) => n.TU);
            VSign = OrientFamily(VCurves, NodesByU(), n => n.V, (n) => n.TV);

            foreach (var n in Nodes)
            {
                n.TU *= USign[n.U];
                n.TV *= VSign[n.V];
            }
        }

        private static int[] OrientFamily(List<Curve> crvs, List<Node>[] crossingLists, Func<Node, int> piece, Func<Node, double> param)
        {
            var sign = new int[crvs.Count];
            var neighbors = Enumerable.Range(0, crvs.Count).Select(_ => new List<Tuple<int, int, double, double>>()).ToArray();
            foreach (var list in crossingLists)
            {
                for (int k = 1; k < list.Count; k++)
                {
                    int a = piece(list[k - 1]);
                    int b = piece(list[k]);
                    if (a == b) { continue; }
                    neighbors[a].Add(Tuple.Create(b, a, param(list[k]), param(list[k - 1])));
                    neighbors[b].Add(Tuple.Create(a, b, param(list[k - 1]), param(list[k])));
                }
            }

            //seed each connected group with the curve's own direction (the input is
            //already roughly oriented by SortCurvesBySurface)
            for (int s = 0; s < crvs.Count; s++)
            {
                if (sign[s] != 0) { continue; }
                sign[s] = 1;
                var queue = new Queue<int>();
                queue.Enqueue(s);
                while (queue.Count > 0)
                {
                    int a = queue.Dequeue();
                    foreach (var nb in neighbors[a])
                    {
                        int b = nb.Item1;
                        if (sign[b] != 0) { continue; }
                        var ta = crvs[a].TangentAt(nb.Item4) * sign[a];
                        var tb = crvs[b].TangentAt(nb.Item3);
                        sign[b] = ta * tb < 0 ? -1 : 1;
                        queue.Enqueue(b);
                    }
                }
            }
            return sign;
        }

        /// <summary>
        /// Give every piece the index of the grid line it is on.
        /// </summary>
        private void AssignLines()
        {
            int rows, cols;
            URow = AssignFamily(UCurves.Count, NodesByV(), n => n.U, out rows);
            VCol = AssignFamily(VCurves.Count, NodesByU(), n => n.V, out cols);
            RowLineCount = rows;
            ColLineCount = cols;
        }

        private static int[] AssignFamily(int count, List<Node>[] crossingLists, Func<Node, int> piece, out int lineCount)
        {
            var index = Enumerable.Repeat(int.MinValue, count).ToArray();
            var steps = Enumerable.Range(0, count).Select(_ => new List<Tuple<int, int>>()).ToArray();
            var hasNodes = new bool[count];
            foreach (var list in crossingLists)
            {
                foreach (var n in list) { hasNodes[piece(n)] = true; }
                for (int k = 1; k < list.Count; k++)
                {
                    int a = piece(list[k - 1]);
                    int b = piece(list[k]);
                    if (a == b) { continue; }
                    steps[a].Add(Tuple.Create(b, 1));
                    steps[b].Add(Tuple.Create(a, -1));
                }
            }

            //walk each connected group; separate groups (e.g. a surface split in two by a
            //hole) are stacked after each other so their cells never overwrite each other
            int offset = 0;
            for (int s = 0; s < count; s++)
            {
                if (!hasNodes[s] || index[s] != int.MinValue) { continue; }
                var group = new List<int>();
                index[s] = 0;
                var queue = new Queue<int>();
                queue.Enqueue(s);
                while (queue.Count > 0)
                {
                    int a = queue.Dequeue();
                    group.Add(a);
                    foreach (var step in steps[a])
                    {
                        if (index[step.Item1] != int.MinValue) { continue; }
                        index[step.Item1] = index[a] + step.Item2;
                        queue.Enqueue(step.Item1);
                    }
                }
                int min = group.Min(g => index[g]);
                int max = group.Max(g => index[g]);
                foreach (var g in group) { index[g] = index[g] - min + offset; }
                offset += max - min + 1;
            }

            for (int i = 0; i < count; i++)
            {
                if (index[i] == int.MinValue) { index[i] = -1; } //piece crosses nothing
            }
            lineCount = offset;
            return index;
        }

        #endregion GRAPH

        #region POINT GRID

        /// <summary>
        /// Intersection points by [row line][column line], Point3d.Unset where the lines
        /// don't meet (holes, trims).
        /// </summary>
        public List<List<Point3d>> PointGrid()
        {
            var grid = new List<List<Point3d>>();
            for (int r = 0; r < RowLineCount; r++)
            {
                grid.Add(Enumerable.Repeat(Point3d.Unset, ColLineCount).ToList());
            }
            foreach (var n in Nodes)
            {
                int r = URow[n.U];
                int c = VCol[n.V];
                if (r < 0 || c < 0) { continue; }
                if (grid[r][c] != Point3d.Unset) { continue; }
                grid[r][c] = n.Point;
            }
            return grid;
        }

        #endregion POINT GRID

        #region QUAD CORNERS

        /// <summary>
        /// Quad corners by [row][column], in the same layout as SurfaceQuadPoints.GetQuadCorners:
        /// (RowLineCount + 1) rows by (ColLineCount + 1) columns, each cell is 5 points
        /// [top left, top right, bottom right, bottom left, extra pentagon corner].
        /// Missing corners are Point3d.Unset.
        /// </summary>
        public List<List<List<Point3d>>> QuadCorners(Brep brep)
        {
            var output = new List<List<List<Point3d>>>();
            for (int r = 0; r < RowLineCount + 1; r++)
            {
                var row = new List<List<Point3d>>();
                for (int c = 0; c < ColLineCount + 1; c++)
                {
                    row.Add(Enumerable.Repeat(Point3d.Unset, 5).ToList());
                }
                output.Add(row);
            }
            if (brep == null || RowLineCount == 0 || ColLineCount == 0) { return output; }

            var nodesByU = NodesByU();
            var nodesByV = NodesByV();
            var cutters = UCurves.Concat(VCurves).ToList();
            var areas = new Dictionary<Tuple<int, int>, double>();

            foreach (var face in brep.Faces)
            {
                var split = face.Split(cutters, _tol);
                if (split == null) { continue; }

                foreach (var cell in split.Faces)
                {
                    int row, col;
                    if (!LocateCell(cell, nodesByU, nodesByV, out row, out col)) { continue; }
                    if (row < 0 || row > RowLineCount || col < 0 || col > ColLineCount) { continue; }

                    var corners = GetCellCorners(cell);
                    if (corners.Count < 3) { continue; }

                    //a hole can split one grid cell into several pieces, keep the biggest
                    var key = Tuple.Create(row, col);
                    var amp = AreaMassProperties.Compute(cell, true, false, false, false);
                    double area = amp != null ? amp.Area : 0;
                    double existing;
                    if (areas.TryGetValue(key, out existing) && existing >= area) { continue; }
                    areas[key] = area;

                    Vector3d du, dv;
                    var center = Average(corners);
                    LocalFrame(center, nodesByU, nodesByV, out du, out dv);
                    output[row][col] = OrderCorners(corners, center, du, dv);
                }
            }

            return output;
        }

        /// <summary>
        /// Find which grid cell a split face belongs to from the curves its edges lie on.
        /// </summary>
        private bool LocateCell(BrepFace cell, List<Node>[] nodesByU, List<Node>[] nodesByV, out int row, out int col)
        {
            row = int.MinValue;
            col = int.MinValue;
            var loop = cell.OuterLoop;
            if (loop == null) { return false; }

            foreach (var trim in loop.Trims)
            {
                var edge = trim.Edge;
                if (edge == null) { continue; }
                var mid = edge.PointAt(edge.Domain.Mid);
                double len = edge.GetLength();
                if (len < _tol * 2) { continue; }

                //Split pulls the curves onto the surface, so an edge can sit well off its
                //input curve when the curves aren't exactly on the surface. Match the edge to
                //the closer family, within a distance that scales with the edge length
                //(an edge on the other family is about half a cell away)
                double maxDist = Math.Max(_tol * 10, len * 0.25);
                double dU, dV;
                int i = ClosestPiece(UCurves, mid, maxDist, out dU);
                int j = ClosestPiece(VCurves, mid, maxDist, out dV);
                if (i >= 0 && j >= 0)
                {
                    if (dU <= dV) { j = -1; } else { i = -1; }
                }

                if (row == int.MinValue)
                {
                    if (i >= 0 && URow[i] >= 0)
                    {
                        //step towards the next U line (along the V curves)
                        var dir = CrossDirection(UCurves[i], USign[i], mid, nodesByU[i], n => VCurves[n.V].TangentAt(n.TV * VSign[n.V]) * VSign[n.V]);
                        int side = SideOfEdge(cell, mid, dir, len);
                        if (side != 0) { row = URow[i] + (side > 0 ? 1 : 0); }
                    }
                }
                if (col == int.MinValue)
                {
                    if (j >= 0 && VCol[j] >= 0)
                    {
                        //step towards the next V line (along the U curves)
                        var dir = CrossDirection(VCurves[j], VSign[j], mid, nodesByV[j], n => UCurves[n.U].TangentAt(n.TU * USign[n.U]) * USign[n.U]);
                        int side = SideOfEdge(cell, mid, dir, len);
                        if (side != 0) { col = VCol[j] + (side > 0 ? 1 : 0); }
                    }
                }
                if (row != int.MinValue && col != int.MinValue) { return true; }
            }
            return false;
        }

        private static int ClosestPiece(List<Curve> crvs, Point3d pt, double maxDist, out double bestDist)
        {
            int best = -1;
            bestDist = maxDist;
            for (int i = 0; i < crvs.Count; i++)
            {
                double t;
                if (!crvs[i].ClosestPoint(pt, out t, bestDist)) { continue; }
                double d = crvs[i].PointAt(t).DistanceTo(pt);
                if (d > bestDist) { continue; }
                bestDist = d;
                best = i;
            }
            return best;
        }

        /// <summary>
        /// Direction across a curve piece towards the next grid line, taken from the
        /// crossing curve at the closest intersection.
        /// </summary>
        private static Vector3d CrossDirection(Curve crv, int sign, Point3d pt, List<Node> nodes, Func<Node, Vector3d> crossingTangent)
        {
            double t;
            crv.ClosestPoint(pt, out t);
            var tangent = crv.TangentAt(t);
            if (nodes.Count == 0) { return Vector3d.Unset; }

            var nearest = nodes.OrderBy(n => n.Point.DistanceTo(pt)).First();
            var dir = crossingTangent(nearest);
            dir -= tangent * (dir * tangent); //perpendicular to the curve piece
            if (!dir.Unitize()) { return Vector3d.Unset; }
            return dir;
        }

        private int SideOfEdge(BrepFace cell, Point3d mid, Vector3d dir, double edgeLength)
        {
            if (!dir.IsValid) { return 0; }

            //step across the edge, getting smaller: a long thin cell (a sliver along a
            //trim) is narrower than a step based on its edge length, so a big step lands
            //outside it on both sides
            double step = Math.Max(edgeLength * 0.05, _tol * 10);
            for (int k = 0; k < 4; k++)
            {
                bool plus = IsOnFace(cell, mid + dir * step);
                bool minus = IsOnFace(cell, mid - dir * step);
                if (plus != minus) { return plus ? 1 : -1; }
                if (step <= _tol * 10) { break; }
                step = Math.Max(step * 0.2, _tol * 10);
            }
            return 0;
        }

        private static bool IsOnFace(BrepFace face, Point3d pt)
        {
            double u, v;
            if (!face.ClosestPoint(pt, out u, out v)) { return false; }
            return face.IsPointOnFace(u, v) == PointFaceRelation.Interior;
        }

        /// <summary>
        /// Corners of a cell: the kinks in its outer boundary (smooth joints along a
        /// hole or trim edge are not corners).
        /// </summary>
        private List<Point3d> GetCellCorners(BrepFace cell)
        {
            var corners = new List<Point3d>();
            var loop = cell.OuterLoop;
            if (loop == null) { return corners; }
            var boundary = loop.To3dCurve();
            if (boundary == null) { return corners; }

            var segments = boundary.DuplicateSegments();
            if (segments == null || segments.Length == 0) { segments = new Curve[] { boundary }; }
            var segs = segments.Where(s => s.GetLength() > _tol).ToList();
            if (segs.Count < 2) { return corners; }

            double kinkAngle = Rhino.RhinoMath.ToRadians(10);
            for (int k = 0; k < segs.Count; k++)
            {
                var prev = segs[(k - 1 + segs.Count) % segs.Count];
                var cur = segs[k];
                var angle = Vector3d.VectorAngle(prev.TangentAtEnd, cur.TangentAtStart);
                if (angle < kinkAngle) { continue; }
                var pt = cur.PointAtStart;
                if (corners.Count > 0 && corners[corners.Count - 1].DistanceTo(pt) < _tol) { continue; }
                corners.Add(pt);
            }
            if (corners.Count > 1 && corners[0].DistanceTo(corners[corners.Count - 1]) < _tol)
            {
                corners.RemoveAt(corners.Count - 1);
            }

            //keep the 5 sharpest corners at most (the cell layout holds 5 points)
            if (corners.Count > 5)
            {
                corners = KeepSharpest(corners, 5);
            }
            return corners;
        }

        private static List<Point3d> KeepSharpest(List<Point3d> corners, int count)
        {
            int n = corners.Count;
            var turn = new double[n];
            for (int k = 0; k < n; k++)
            {
                var a = corners[k] - corners[(k - 1 + n) % n];
                var b = corners[(k + 1) % n] - corners[k];
                turn[k] = Vector3d.VectorAngle(a, b);
            }
            var keep = Enumerable.Range(0, n).OrderByDescending(k => turn[k]).Take(count).OrderBy(k => k);
            return keep.Select(k => corners[k]).ToList();
        }

        private void LocalFrame(Point3d center, List<Node>[] nodesByU, List<Node>[] nodesByV, out Vector3d du, out Vector3d dv)
        {
            du = Vector3d.XAxis;
            dv = Vector3d.YAxis;
            if (Nodes.Count == 0) { return; }
            var nearest = Nodes.OrderBy(n => n.Point.DistanceTo(center)).First();
            du = UCurves[nearest.U].TangentAt(nearest.TU * USign[nearest.U]) * USign[nearest.U];
            dv = VCurves[nearest.V].TangentAt(nearest.TV * VSign[nearest.V]) * VSign[nearest.V];
        }

        /// <summary>
        /// Put the cell corners in the [top left, top right, bottom right, bottom left, extra]
        /// layout, keeping their order around the boundary.
        /// </summary>
        private static List<Point3d> OrderCorners(List<Point3d> corners, Point3d center, Vector3d du, Vector3d dv)
        {
            var result = Enumerable.Repeat(Point3d.Unset, 5).ToList();
            int n = corners.Count;

            //local coordinates, scaled so skinny cells still score by diagonal
            var x = corners.Select(p => (p - center) * du).ToArray();
            var y = corners.Select(p => (p - center) * dv).ToArray();
            double sx = Math.Max(x.Max(Math.Abs), 1e-12);
            double sy = Math.Max(y.Max(Math.Abs), 1e-12);
            //slot 0 TL(-,+) 1 TR(+,+) 2 BR(+,-) 3 BL(-,-)
            double[] slotX = { -1, 1, 1, -1 };
            double[] slotY = { 1, 1, -1, -1 };
            Func<int, int, double> score = (slot, k) => slotX[slot] * x[k] / sx + slotY[slot] * y[k] / sy;

            int m = Math.Min(n, 4);
            double bestScore = double.NegativeInfinity;
            int[] bestSlots = null;
            int[] bestCorners = null;

            foreach (var slots in Combinations(4, m))
            {
                foreach (var picked in Combinations(n, m))
                {
                    for (int rot = 0; rot < m; rot++)
                    {
                        for (int dir = 0; dir < 2; dir++)
                        {
                            var mapped = new int[m];
                            double s = 0;
                            for (int k = 0; k < m; k++)
                            {
                                int idx = dir == 0 ? (k + rot) % m : (rot - k + m) % m;
                                mapped[k] = picked[idx];
                                s += score(slots[k], mapped[k]);
                            }
                            if (s <= bestScore) { continue; }
                            bestScore = s;
                            bestSlots = slots;
                            bestCorners = mapped;
                        }
                    }
                }
            }

            if (bestSlots == null) { return result; }
            for (int k = 0; k < m; k++) { result[bestSlots[k]] = corners[bestCorners[k]]; }

            //pentagon: the corner that wasn't placed goes in the extra slot
            if (n == 5)
            {
                var used = new HashSet<int>(bestCorners);
                for (int k = 0; k < n; k++)
                {
                    if (used.Contains(k)) { continue; }
                    result[4] = corners[k];
                }
            }
            return result;
        }

        private static IEnumerable<int[]> Combinations(int n, int k)
        {
            var idx = Enumerable.Range(0, k).ToArray();
            if (k > n) { yield break; }
            while (true)
            {
                yield return (int[])idx.Clone();
                int i = k - 1;
                while (i >= 0 && idx[i] == n - k + i) { i--; }
                if (i < 0) { yield break; }
                idx[i]++;
                for (int j = i + 1; j < k; j++) { idx[j] = idx[j - 1] + 1; }
            }
        }

        private static Point3d Average(List<Point3d> pts)
        {
            var sum = Point3d.Origin;
            foreach (var p in pts) { sum += p; }
            return sum / pts.Count;
        }

        #endregion QUAD CORNERS
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

namespace yunggh
{
    internal partial class YungGH
    {
        public static List<GH_Surface> PQMeshSurface(GH_Surface srf, IEnumerable<GH_Curve> rails1, IEnumerable<GH_Curve> rails2)
        {
            List<GH_Surface> panels = new List<GH_Surface>();
            if (srf == null || rails1 == null || rails2 == null) { return panels; }
            if (rails1.Count() == 0 || rails2.Count() == 0) { return panels; }

            Surface surface = null;
            if (!GH_Convert.ToSurface(srf, ref surface, GH_Conversion.Both)) { return panels; }

            //get all intersection points
            var points = new List<Point3d>();
            foreach (GH_Curve rail1 in rails1)
            {
                foreach (GH_Curve rail2 in rails2)
                {
                    var ccx = Rhino.Geometry.Intersect.Intersection.CurveCurve(rail1.Value, rail2.Value, 0.001, 0);
                    if (ccx == null) { continue; }
                    if (ccx.Count == 0) { continue; }

                    //get intersection as surface parameter
                    points.Add(ccx[0].PointA);
                }
                //TODO: add check to make sure rails are isocurves
                points.Add(rail1.Value.PointAtStart);
                points.Add(rail1.Value.PointAtEnd);
            }
            foreach (GH_Curve rail2 in rails2)//add end points from other rails
            {
                points.Add(rail2.Value.PointAtStart);
                points.Add(rail2.Value.PointAtEnd);
            }
            points = points.Distinct().ToList();

            //convert intersection points to surface UV coordinates
            var uvs = new List<Point2d>();
            foreach (Point3d pt in points)
            {
                double u; double v;
                if (!surface.ClosestPoint(pt, out u, out v)) { continue; }
                u = Math.Round(u, 3);
                v = Math.Round(v, 3);
                uvs.Add(new Point2d(u, v));
            }

            //sort/organize intersection points into order columns
            var sortedPoints = new SortedDictionary<double, List<Point2d>>();
            foreach (Point2d uv in uvs)
            {
                double key = uv.X;
                if (!sortedPoints.ContainsKey(key))
                {
                    sortedPoints.Add(key, new List<Point2d>());
                }
                sortedPoints[key].Add(uv);
            }
            //sort each column by the opposite point value
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                var kvp = sortedPoints.ElementAt(i);
                var pts = kvp.Value;
                pts = pts.OrderBy(o => o.Y).Distinct().ToList();
                sortedPoints[kvp.Key] = pts;
            }

            //TODO: create panels

            return panels;
        }

        /// <summary>
        /// Find the largest Brep by volume in an array of Breps.
        /// </summary>
        /// <param name="inputBreps">Breps to evaluate</param>
        /// <returns></returns>
        public Brep FindLargestBrepByVolume(Brep[] inputBreps)
        {
            Brep largestBrep = null;
            double largestVolume = 0;
            foreach (Brep inputBrep in inputBreps)
            {
                double currentVolume = inputBrep.GetVolume();
                if (currentVolume > largestVolume)
                {
                    largestVolume = currentVolume;
                    largestBrep = inputBrep;
                }
            }
            return largestBrep;
        }

        /// <summary>
        /// Retrim Surface to minimum.
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <param name="shrinkSides">Sides to srhink</param>
        /// <returns>Brep</returns>
        public Brep ShrinkSurface(Brep surface, int shrinkSides = 0)
        {
            //guard statement for breps without faces
            if (surface.Faces.Count == 0) return null;

            BrepFace face = surface.Faces[0];
            if (shrinkSides <= 0) { face.ShrinkFace(BrepFace.ShrinkDisableSide.ShrinkAllSides); }
            else if (shrinkSides == 1)
            { face.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkNorthSide); }
            else if (shrinkSides == 2) { face.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkEastSide); }
            else if (shrinkSides == 3) { face.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkSouthSide); }
            else if (shrinkSides >= 4) { face.ShrinkFace(BrepFace.ShrinkDisableSide.DoNotShrinkWestSide); }

            Brep brep = face.Brep;
            return brep;
        }

        /// <summary>
        /// Retrim Surface to minimum.
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <returns>Brep</returns>
        public Brep ShrinkSurface(Brep surface)
        {
            //guard statement for breps without faces
            if (surface.Faces.Count == 0) return null;

            BrepFace face = surface.Faces[0];
            face.ShrinkSurfaceToEdge();
            Brep brep = face.Brep;
            return brep;
        }

        /// <summary>
        /// Fits a bounding box based on largest face and longest edge.
        /// </summary>
        /// <param name="B">A brep to fit inside an oriented bounding box</param>
        /// <param name="plane">returns a plane of the bounding boxes orientation</param>
        /// <param name="normal">returns the normal of the bounding boxes plane</param>
        /// <param name="forward">returns the forward direction of the bounding box</param>
        /// <returns>A bounding box fitted around the brep</returns>
        public Box FitBoundingBox(Brep B, out Plane plane, out Vector3d normal, out Vector3d forward)
        {
            //get surface normal from largest surface of brep
            Point3d origin = new Point3d(0, 0, 0);
            normal = new Vector3d(0, 0, 1);
            double largestArea = 0;
            bool NoPlanarSurfacesFound = true;
            foreach (BrepFace brep in B.Faces)
            {
                if (!brep.IsPlanar()) continue;// we only want to use planar surfaces

                Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(brep);
                if (area.Area < largestArea) continue;

                largestArea = area.Area;
                origin = Rhino.Geometry.AreaMassProperties.Compute(brep).Centroid;

                double u; double v;
                brep.ClosestPoint(origin, out u, out v);
                normal = brep.NormalAt(u, v); //set the normal of the largest surface
                NoPlanarSurfacesFound = false;
            }

            if (NoPlanarSurfacesFound)
            {
                foreach (BrepFace brep in B.Faces)
                {
                    Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(brep);
                    if (area.Area <= largestArea) continue;

                    largestArea = area.Area;
                    origin = Rhino.Geometry.AreaMassProperties.Compute(brep).Centroid;

                    double u; double v;
                    brep.ClosestPoint(origin, out u, out v);
                    normal = brep.NormalAt(u, v); //set the normal of the largest surface
                    NoPlanarSurfacesFound = false;
                }
            }

            //get forward direction vector from longest line of brep
            forward = new Vector3d(0, 0, 0);
            double longestLength = 0;
            foreach (Curve crv in B.Edges)
            {
                //we are only interested in linear curves
                if (!crv.IsLinear()) continue;

                double length = crv.GetLength();
                if (length < longestLength) continue;

                longestLength = length;
                forward = crv.PointAtEnd - crv.PointAtStart;
            }

            //contruct orientation plane from normal and forward vector
            plane = new Plane(origin, normal);
            forward.Transform(Rhino.Geometry.Transform.PlanarProjection(plane));
            forward.Unitize();
            double angle = Vector3d.VectorAngle(plane.YAxis, forward, plane);
            plane.Rotate(angle, normal);

            //create bounding box
            Box worldBox;
            BoundingBox box = B.GetBoundingBox(plane, out worldBox);

            return worldBox;
        }

        /// <summary>
        /// Test a surface for developability status.
        /// </summary>
        /// <param name="surface">Surface to test.</param>
        /// <param name="type">Developability type as index</param>
        /// <returns>Developability type as string</returns>
        public string TestSurfaceDevelopability(Surface surface, out int type)
        {
            //https://discourse.mcneel.com/t/verifying-developable-surfaces/73594
            //https://discourse.mcneel.com/t/ruling-line-from-edge-curves-twist-check/73952

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //planar
            if (surface.IsPlanar(tolerance)) { type = 0; return "planar"; }

            //cylinder
            if (surface.IsCylinder(tolerance)) { type = 1; return "cylinder"; }

            //conic
            if (surface.IsCone(tolerance)) { type = 2; return "conic"; }

            //spheric
            if (surface.IsSphere(tolerance) || surface.IsTorus(tolerance)) { type = 4; return "double curved"; }

            //ruled surface

            Rhino.Geometry.Unroller unroll = null;
            Rhino.Geometry.Surface srf = surface;
            if (srf != null)
                unroll = new Rhino.Geometry.Unroller(srf);

            Rhino.Geometry.Brep brep = surface.ToBrep();

            if (unroll == null)
            {
                type = 4;
                return "double curved";
            }
            else
            {
                type = 3;
                return "ruled surface";
            }
        }

        /// <summary>
        /// Finds the extremums of a brep.
        /// </summary>
        /// <param name="brep">Brep</param>
        /// <param name="directions">Extremum direction(s)</param>
        /// <param name="minmax">True for maximum, false for minimum</param>
        /// <returns>List of points/curves representing the brep's extremum(s)</returns>
        public List<Object> FindExtremums(Brep brep, List<Vector3d> directions, bool minmax)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Object> extremums = new List<Object>();
            //Rhino.Geometry.GeometryBase
            foreach (Vector3d normal in directions)
            {
                Plane plane = new Plane(new Point3d(0, 0, 0), normal);

                //create bounding box
                Box worldBox;
                BoundingBox box = brep.GetBoundingBox(plane, out worldBox);

                //using the world box corners, we create top or bottom planes
                Point3d[] corners = worldBox.GetCorners();
                Plane intersection = new Plane(corners[0], corners[1], corners[2]); //bottom plane
                if (!minmax)
                    intersection = new Plane(corners[4], corners[5], corners[6]); //top plane

                Curve[] crvs;
                Point3d[] pts;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, intersection, 0, out crvs, out pts)) { continue; }

                foreach (Curve crv in crvs)
                    extremums.Add(crv);
                foreach (Point3d pt in pts)
                    extremums.Add(pt);

                if (crvs.Length > 0 || pts.Length > 0) continue;

                //if no curve intersections were found, we need to get all the edges of the brep and check for intersections.
                List<Point3d> Xpts = new List<Point3d>();
                foreach (Curve edg in brep.Edges)
                {
                    Rhino.Geometry.Intersect.CurveIntersections Xcrv = Rhino.Geometry.Intersect.Intersection.CurvePlane(edg, intersection, tolerance);
                    if (Xcrv == null) continue;

                    for (int i = 0; i < Xcrv.Count; i++)
                    {
                        Rhino.Geometry.Intersect.IntersectionEvent crvX = Xcrv[i];
                        Xpts.Add(crvX.PointA);
                    }
                }
                Point3d[] culledXpts = Point3d.CullDuplicates(Xpts, tolerance);
                foreach (Point3d pt in culledXpts)
                    extremums.Add(pt);
            }

            return extremums;
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="P"> List of planes to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        public List<Brep> SafeSplit(List<Brep> breps, List<Plane> P, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (P.Count == 0) return breps;

            Plane plane = P[0];
            P.RemoveAt(0);
            foreach (Brep brep in breps)
            {
                Curve[] intersections;
                Point3d[] pt;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, plane, tolerance, out intersections, out pt)) { splits.Add(brep); continue; }

                //if the intersection failed, we keep the original brep
                if (intersections.Length == 0) { splits.Add(brep); continue; }

                Brep[] test = brep.Split(intersections, tolerance); //it will always only be one intersection at a time

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (P.Count != 0)
            {
                splits = SafeSplit(splits, P, tolerance);
            }

            return splits;
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="C"> List of curves to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        public List<Brep> SafeSplit(List<Brep> breps, List<Curve> C, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (C.Count == 0) return breps;

            Curve crv = C[0];
            C.RemoveAt(0);
            foreach (Brep brep in breps) //TODO: loop splits if curve didn't split,
            {
                Brep[] test = brep.Split(new List<Curve>() { crv }, tolerance); //it will always only be one intersection at a time

                //if curve is planar, we'll try splitting it with a plane
                if (test.Length == 0 && crv.IsPlanar())
                {
                    Plane plane = Plane.Unset;
                    if (!crv.TryGetPlane(out plane)) { splits.Add(brep); continue; }

                    if (!plane.IsValid) { splits.Add(brep); continue; }

                    List<Brep> temp = SafeSplit(new List<Brep>() { brep }, new List<Plane>() { plane }, tolerance);

                    foreach (Brep b in temp)
                        splits.Add(b);
                    continue;
                }

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (C.Count != 0)
            {
                splits = SafeSplit(splits, C, tolerance);
            }

            return splits;
        }

        /// <summary>
        /// Checks which breps a list of points is touching
        /// </summary>
        /// <param name="breps">List of breps for testing point touch</param>
        /// <param name="pts">List of points to check if brep is touching</param>
        /// <param name="tolerance"> Tolerance for point check</param>
        /// <returns>The indices of the breps touched by points</returns>
        public List<int> BrepPointCheck(List<Brep> breps, List<Point3d> pts, double tolerance)
        {
            List<int> indices = new List<int>();

            foreach (Point3d pt in pts)
            {
                for (int i = 0; i < breps.Count; i++)
                {
                    Brep brep = breps[i];
                    Point3d cp = brep.ClosestPoint(pt);
                    double dist = pt.DistanceTo(cp);
                    if (dist <= tolerance)
                    {
                        indices.Add(i);
                    }
                }
            }
            return indices;
        }
    }
}
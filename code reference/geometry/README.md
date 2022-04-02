# geometry

1. <a href="#tag-vector3d">Vector3d</a>
1. <a href="#tag-point3d">Point3d</a>
1. <a href="#tag-line">Line</a>
1. <a href="#tag-plane">Plane</a>
1. <a href="#tag-polyline">Polyline</a>
1. <a href="#tag-nurbscurve">NurbsCurve</a>
1. <a href="#tag-boundingbox">BoundingBox</a>
1. <a href="#tag-box">Box</a>
1. <a href="#tag-brep">Brep</a>
1. <a href="#tag-curve">Curve</a>
1. <a href="#tag-surface">Surface</a>

# <a id="tag-vector3d" href="#tag-vector3d">Vector3d</a>
[Rhino Vector3d documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Vector3d.htm)

```csharp
	void RunScript()
	{
		Vector3d vec1 = new Vector3d(0,0,1);
		Vector3d vec2 = new Vector3d(0,1,0);
		double length = vec1.Length;
		bool isPerp = vec1.IsPerpendicularTo(vec2);
		if(isPerp){Print(vec2.ToString());}
	}
```

# <a id="tag-point3d" href="#tag-point3d">Point3d</a>
[Rhino Point3d documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Point3d.htm)

```csharp
	void RunScript()
	{
		Point3d pt1 = new Point3d(0,0,1);
		Point3d pt2 = new Point3d(0,1,0);
		double z = pt1.Z;
		pt1.Z = z * 2;
		double dist = pt1.DistanceTo(pt2);
		if(dist > 2){Print(pt1.ToString());}
	}

	//Console:
	//0,0,2
```

# <a id="tag-line" href="#tag-line">Line</a>
[Rhino Line documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Line.htm)

```csharp
	void RunScript()
	{
		Point3d pt1 = new Point3d(0,0,2);
		Point3d pt2 = new Point3d(0,2,0);
		Line line = new Line(pt1, pt2);
		Vector3d dir = line.Direction;
		Print(dir.ToString());
		Point3d pAt = line.PointAt(0.5);
		Print(pAt.ToString());}
	}

	//Console:
	//0,2,-2
	//0,1,-1
```

# <a id="tag-plane" href="#tag-plane">Plane</a>
[Rhino Plane documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Plane.htm)

```csharp
	void RunScript()
	{
		Point3d pt1 = new Point3d(0,0,2);
		Point3d pt2 = new Point3d(0,2,0);
		Plane pln1 = new Plane(Point3d.Origin,Vector3d.ZAxis);
		Plane pln2 = new Plane(pt1,Vector3d.XAxis,Vector3d.YAxis);
		Plane pln3 = new Plane(Point3d.Origin,pt1,pt2);
		Vector3d normal = pln3.Normal;
		Print(normal.ToString());}
	}

	//Console:
	//-1,0,0
```

# <a id="tag-polyline" href="#tag-polyline">Polyline</a>
[Rhino Polyline documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Polyline.htm)

```csharp
	void RunScript(List<Point3d> pts)
	{
		Polyline poly = new Polyline();
		for(int i = 0;i < pts.Count; i++)
		{
			poly.Add(pts[i]);
		}
	}
```

```csharp
	void RunScript(List<Point3d> pts)
	{
		Polyline poly = new Polyline();
		foreach(Point3d pt in pts)
		{
				poly.Add(pt);
		}
	}
```

```csharp
	void RunScript(List<Point3d> pts)
	{
		Polyline poly = new Polyline();
		poly.AddRange(pts);
	}
```

# <a id="tag-nurbscurve" href="#tag-nurbscurve">NurbsCurve</a>
[Rhino NurbsCurve documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_NurbsCurve.htm)

```csharp
	void RunScript(List<Point3d> pts)
	{
		Polyline poly = new Polyline();
		poly.AddRange(pts);
		int count = poly.Count;
		double length = poly.Length;
		NurbsCurve curve = poly.ToNurbsCurve();
	}
```

# <a id="tag-boundingbox" href="#tag-boundingbox">BoundingBox</a>
[Rhino BoundingBox documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_BoundingBox.htm)

```csharp
	void RunScript(ref object A)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Print(bb.ToString());
		A = bb;
	}
	//Console:
	//0,0,0 - 10,10,10
```

# <a id="tag-box" href="#tag-box">Box</a>
[Rhino Box documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Box.htm)

```csharp
	void RunScript(ref object A)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Box box = new Box(Plane.WorldXY, new Point3d[]{pt0,pt1});
		Print(box.ToString());
		A = box;
	}
	//Console:
	//Rhino.Geometry.Box
```

# <a id="tag-brep" href="#tag-brep">Brep</a>
[Rhino Brep documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Brep.htm)

```csharp
	void RunScript(ref object A)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Box box = new Box(Plane.WorldXY, new Point3d[]{pt0,pt1});
		Brep brep = box.ToBrep();
		Print(brep.ToString());
		A = brep;
	}
	//Console:
	//Rhino.Geometry.Brep
```

# <a id="tag-curve" href="#tag-curve">Curve</a>
[Rhino Curve documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Curve.htm)

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepCurveList crvs = brep.Curves3D;
		Curve crv = crvs[8];
		A = brep;
		B = crv;
	}
```

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepSurfaceList srfs = brep.Surfaces;
		Surface srf= srfs[5];
		Interval u = srf.Domain(0);
		Curve crv = srf.IsoCurve(0, u.ParameterAt(0.75));
		A = brep;
		B = crv;
	}
```

# <a id="tag-surface" href="#tag-surface">Surface</a>
[Rhino Surface documentation](https://developer.rhino3d.com/api/RhinoCommon/html/T_Rhino_Geometry_Surface.htm)

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepSurfaceList srfs = brep.Surfaces;
		Surface srf= srfs[0];
		A = brep;
		B = srf;
	}
```
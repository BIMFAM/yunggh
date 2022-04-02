# transforms

1. <a href="#tag-translation">Translation</a>
1. <a href="#tag-rotation">Rotation</a>
1. <a href="#tag-scale">Scale</a>
1. <a href="#tag-projection">Projection</a>

# <a id="tag-translation" href="#tag-translation">Translation</a>
[Rhino Translation documentation](https://developer.rhino3d.com/api/RhinoCommon/html/M_Rhino_Geometry_Transform_Translation.htm)

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepSurfaceList srfs = brep.Surfaces;
		Brep trans = brep.DuplicateBrep();
		Transform xform = Transform.Translation(new Vector3d(1, 1, 1));
		trans.Transform(xform);
		A = brep;
		B = trans;
	}
```

# <a id="tag-rotation" href="#tag-rotation">Rotation</a>
[Rhino Rotation documentation](https://developer.rhino3d.com/api/RhinoCommon/html/Overload_Rhino_Geometry_Transform_Rotation.htm)

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepSurfaceList srfs = brep.Surfaces;
		Brep trans = brep.DuplicateBrep();
		Transform xform = Transform.Rotation(Math.PI * 0.25, bb.Center);
		trans.Transform(xform);
		A = brep;
		B = trans;
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
		Brep trans = brep.DuplicateBrep();
		Transform xform = Transform.Rotation(
		Math.PI * 0.25, Vector3d.YAxis, bb.Center);
		trans.Transform(xform);
		A = brep;
		B = trans;
	}
```

# <a id="tag-scale" href="#tag-scale">Scale</a>
[Rhino Scale documentation](https://developer.rhino3d.com/api/RhinoCommon/html/Overload_Rhino_Geometry_Transform_Scale.htm)

```csharp
	void RunScript(ref object A, ref object B)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Rhino.Geometry.Collections.BrepSurfaceList srfs = brep.Surfaces;
		Brep trans = brep.DuplicateBrep();
		Transform xform = Transform.Scale(bb.Center, 0.5);
		trans.Transform(xform);
		A = brep;
		B = trans;
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
		Brep trans = brep.DuplicateBrep();
		Transform xform = Transform.Scale(Plane.WorldXY, 1.1, 1.1, -0.1);
		trans.Transform(xform);
		A = brep;
		B = trans;
	}
```

# <a id="tag-projection" href="#tag-projection">Projection</a>
[Rhino Projection documentation](https://developer.rhino3d.com/api/RhinoCommon/html/M_Rhino_Geometry_Transform_PlanarProjection.htm)

```csharp
	void RunScript(ref object A, ref object B, ref object C)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Surface srf = brep.Surfaces[5];
		Curve crv = srf.IsoCurve(0, 5);
		Brep trans = crv.DuplicateCurve();
		Plane plane = Plane.WorldXY
		Transform xform = Transform.PlanarProjection(plane);
		trans.Transform(xform);
		A = brep; B = crv; C = trans;
	}
```

```csharp
	void RunScript(ref object A, ref object B, ref object C)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Surface srf = brep.Surfaces[5];
		Curve crv = srf.IsoCurve(0, 5);
		Brep trans = crv.DuplicateCurve();
		Plane plane = new Plane(bb.Center, new Vector3d(0, 0, 1));
		Transform xform = Transform.PlanarProjection(plane);
		trans.Transform(xform);
		A = brep; B = crv; C = trans;
	}
```

```csharp
	void RunScript(ref object A, ref object B, ref object C)
	{
		Point3d pt0 = new Point3d(0, 0, 0);
		Point3d pt1 = new Point3d(10, 10, 10);
		BoundingBox bb = new BoundingBox(pt0, pt1);
		Brep brep = bb.ToBrep();
		Surface srf = brep.Surfaces[5];
		Curve crv = srf.IsoCurve(0, 5);
		Brep trans = crv.DuplicateCurve();
		Plane plane = new Plane(bb.Center, new Vector3d(1, 0, 1));
		Transform xform = Transform.PlanarProjection(plane);
		trans.Transform(xform);
		A = brep; B = crv; C = trans;
	}
```
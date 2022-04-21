# data sorting

1. <a href="#tag-reverse">Reverse</a>
1. <a href="#tag-sort">Sort</a>
1. <a href="#tag-orderby">OrderBy</a>

# <a id="tag-reverse" href="#tag-reverse">Reverse</a>
[Microsoft Reverse documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.reverse?view=net-6.0)

```csharp
	List<int> nums = new List<int>();
	nums.Add(16);
	nums.Add(8);
	nums.Add(32);
	nums.Add(4);

	nums.Reverse();

	Print(nums[1]);

	//Console:
	//32
```

# <a id="tag-sort" href="#tag-sort">Sort</a>
[Microsoft Sort documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.sort?view=net-6.0)

```csharp
	List<int> nums = new List<int>();
	nums.Add(16);
	nums.Add(8);
	nums.Add(32);
	nums.Add(4);

	nums.Sort();

	Print(nums[0]);

	//Console:
	//4
```

# <a id="tag-orderby" href="#tag-orderby">OrderBy</a>
[Linq OrderBy documentation](https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderby?view=net-6.0)
[OrderBy Stackoverflow](https://stackoverflow.com/questions/3309188/how-to-sort-a-listt-by-a-property-in-the-object)

```csharp
	using System.Linq;
	
	List<Point3d> points = new List<Point3d>();
	points.Add(new Point3d(4,0,0));
	points.Add(new Point3d(7,0,0));
	points.Add(new Point3d(2,0,0));
	
	List<Point3d> sortedPoints = points.OrderBy(o=>o.X).ToList();
	
	Print(sortedPoints[0].X.ToString());
	Print(sortedPoints[1].X.ToString());
	Print(sortedPoints[2].X.ToString());
	
	//Console:
	//2
	//4
	//7
```
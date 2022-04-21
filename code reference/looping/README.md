# looping

1. <a href="#tag-foreach">foreach</a>
1. <a href="#tag-for">for</a>
1. <a href="#tag-backwardsfor">backwards for</a>
1. <a href="#tag-nestedfor">nested for</a>
1. <a href="#tag-dictionaryiteration">Dictionary iteration</a>
1. <a href="#tag-datatreeiteration">DataTree iteration</a>
1. <a href="#tag-recursivefunctions">Recursive Functions</a>
1. <a href="#tag-while">while</a>
1. <a href="#tag-parallelfor">Parallel For</a>

# <a id="tag-foreach" href="#tag-foreach">foreach</a>
[Microsoft foreach documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-foreach-statement)

```csharp
	List<int> nums = new List<int>(){0,2,4,7};

	foreach(int i in nums)
	{
		Print(i);
	}

	//Console:
	//0
	//2
	//4
	//7
```

# <a id="tag-for" href="#tag-for">for</a>
[Microsoft for documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-for-statement)

```csharp
	List<int> nums = new List<int>(){0,2,4,7};

	for(int i = 0;i < nums.Count; i++)
	{
		Print(nums[i]);
	}

	//Console:
	//0
	//2
	//4
	//7
```

```csharp
	int[] nums = new int[]{0,2,4,7};

	int n = 0;
	for(int i = 0;i < nums.Length; i++)
	{
		n += nums[i];
		nums[i] = n;
		Print(nums[i]);
	}

	//Console:
	//0
	//2
	//6
	//13
```

# <a id="tag-backwardsfor" href="#tag-backwardsfor">backwards for</a>
[Microsoft for documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-for-statement)

```csharp
	List<int> nums = new List<int>(){0,2,4,7};

	for(int i = nums.Count - 1;i >= 0; i--)
	{
		Print(nums[i]);
	}

	//Console:
	//7
	//4
	//2
	//0
```

# <a id="tag-nestedfor" href="#tag-nestedfor">nested for</a>
[Microsoft for documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-for-statement)

```csharp
	List<int> nums = new List<int>(){0,2,4,7};

	for(int i = 0;i < nums.Count; i++)
	{
		for(int j = 0;j < nums.Count; j++)
		{
			Print(nums[i] + ", " + nums[j]);
		}
	}

	//Console:
	//0, 0
	//0, 2
	//0, 4
	//0, 7
	//2, 0
	//2, 2
	//2, 4
	//2, 7
	//4, 0
	//4, 2
	//4, 4
	//4, 7
	//7, 0
	//7, 2
	//7, 4
	//7, 7
```

# <a id="tag-dictionaryiteration" href="#tag-dictionaryiteration">Dictionary iteration</a>
[Microsoft Dictionary documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-6.0)

```csharp
	void RunScript()
	{
		Dictionary<string,string> loc = new Dictionary<string,string>();
		loc.Add("CA","LA");
		loc.Add("NY","NYC");
		loc.Add("IL","CHI");
		loc.Add("MA","BOS");
	
		foreach(KeyValuePair<string,string> kvp in loc)
		{
			Print(kvp.Value);
		}
	}
	//LA
	//NYC
	//CHI
	//BOS
```

```csharp
	void RunScript()
	{
		Dictionary<string,string> loc = new Dictionary<string,string>();
		loc.Add("CA","LA");
		loc.Add("NY","NYC");
		loc.Add("IL","CHI");
		loc.Add("MA","BOS");
	
		List<string> keys = loc.Keys.ToList();
		for(int i = 0;i<keys.Count;i++)
		{
			string key = keys[i];
			string v = loc[key];
			Print(v);
		}
	}
	//LA
	//NYC
	//CHI
	//BOS
```

# <a id="tag-datatreeiteration" href="#tag-datatreeiteration">DataTree iteration</a>
[Rhino DataTree documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-6.0)

```csharp
	private void RunScript(DataTree<Point3d> P)
	{
		DataTree<int> pZ = new DataTree<int>();
		for(int p = 0;p < P.BranchCount;p++)
		{
			GH_Path path = P.Path(i);
			List<Point3d> pts = P.Branch(p);
			List<int> z = new List<int>();
			foreach(Point3d pt in pts)
			{
				z.Add(pt.Z);
			}
			pZ.AddRange(z,path);
		}
	}
```

# <a id="tag-recursivefunctions" href="#tag-recursivefunctions">Recursive Functions</a>
[Microsoft Recursive Functions documentation](https://docs.microsoft.com/en-us/cpp/c-language/recursive-functions?view=msvc-170)

```csharp
	void RunScript()
	{
		Print(Fib(9));
	}

	int Fib(int n)
	{
		if(n <= 1)
	{
	return n;
	}
		return Fib(n - 1) + Fib(int n - 2);
	}

	//Console:
	//34
	//Fibonacci Recursion: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144,...
```

# <a id="tag-while" href="#tag-while">while</a>
[Microsoft while statement documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements#the-while-statement)

```csharp
	void RunScript()
	{
		List<int> nums = new List<int>(){0,2,4,7};
		
		int count = 0;
		int index = 0;
		while(count < 5)
		{
			count += nums[index];
			index++;
			Print(count.ToString());
		}
		Print("END");
	}

	//Console:
	//0
	//2
	//6
	//END
```

# <a id="tag-parallelfor" href="#tag-parallelfor">Parallel For</a>
[Microsoft Parallel For documentation](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-write-a-simple-parallel-for-loop)

```csharp
	private void RunScript(List<Point3d> x, List<Point3d> y, ref object A)
	{

		Point3d[] output = new Point3d[x.Count]; //thread safe
		Parallel.For(0, x.Count, i =>
		{
			Point3d a = x[i];
			Point3d cp = y[0];
			double dist = a.DistanceTo(cp);
			
			for(int j = 1;j < y.Count;j++)
			{
				double tempDist = a.DistanceTo(y[j]);
				if(tempDist > dist){continue;}
				dist = tempDist;
				cp = y[j];
			}
			
			output[i] = cp;
		});

		A = output;
	}
```
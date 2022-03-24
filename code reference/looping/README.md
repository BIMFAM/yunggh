# looping

foreach loop

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

for loop

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

reverse for

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

nested looping

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

dictionary

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

data tree

```csharp

```

recursion

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

parallel processing

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
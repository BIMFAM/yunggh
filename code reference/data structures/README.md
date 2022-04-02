# data structures

1. <a href="#tag-array">Array</a>
1. <a href="#tag-list">List</a>
1. <a href="#tag-reverse">Reverse</a>
1. <a href="#tag-sort">Sort</a>
1. <a href="#tag-dictionary">Dictionary</a>
1. <a href="#tag-datatree">DataTree</a>

# <a id="tag-array" href="#tag-array">Array</a>
[Microsoft Array documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/arrays/)

```csharp
	int[] nums = new int[]{0,2,4,7};

	Print(nums[2]);
	Print(nums[1]);
	Print(nums[3]);
	Print(nums[0]);

	//Console:
	//4
	//2
	//7
	//0
```

# <a id="tag-list" href="#tag-list">List</a>
[Microsoft List documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-6.0)

```csharp
	List<int> nums = new List<int>(){0,2,4};
	nums.Add(7);
	
	Print(nums[0]);
	Print(nums[1]);
	Print(nums[2]);
	Print(nums[3]);

	//Console:
	//0
	//2
	//4
	//7
```

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

# <a id="tag-dictionary" href="#tag-dictionary">Dictionary</a>
[Microsoft Dictionary documentation](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-6.0)

```csharp
	Dictionary<int,string> loc = new Dictionary<int,string>();
	loc.Add(0,"LA");
	loc.Add(1,"NYC");
	loc.Add(2,"CHI");
	loc.Add(3,"BOS");

	Print(loc[0]);

	//Console:
	//LA
```

```csharp
	Dictionary<string,string> loc = new Dictionary<string,string>();
	loc.Add("CA","LA");
	loc.Add("NY","NYC");
	loc.Add("IL","CHI");
	loc.Add("MA","BOS");

	Print(loc["MA"]);

	//Console:
	//BOS
```

# <a id="tag-datatree" href="#tag-datatree">DataTree</a>
[Rhino DataTree documentation](https://developer.rhino3d.com/api/grasshopper/html/T_Grasshopper_DataTree_1.htm)

```csharp
	List<int> nums = new List<int>(){0,1,2};
    DataTree<int> numTree = new DataTree<int>();
    foreach(int i in nums)
    {
      GH_Path path = new GH_Path(i);
	  Print(path.ToString());
      numTree.Add(i, path);
    }
    //Console: {0}
	//Console: {1}
	//Console: {2}
```
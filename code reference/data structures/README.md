# data structures

Array

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

List

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

Reverse

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

Sort

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

Dictionary

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

DataTree
# conditional looping

1. <a href="#tag-forif">for if</a>

# <a id="tag-forif" href="#tag-if">for if</a>
[Microsoft Iteration documentation](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/statements/iteration-statements)

```csharp
	List<int> nums = new List<int>(){2,4,6,8};
	for(int i = 0;i < nums.Count; i++)
	{
		if(nums[i] < 5)
		{
			Debug.Log("Less");
		}
	}
	//Console:
	//Less
	//Less
```

```csharp
	List<int> nums = new List<int>(){2,4,6,8};
	for(int i = 0;i < nums.Count; i++)
	{
		if(nums[i] < 5)
		{
			Debug.Log(i + ", Less");
		}
	}
	//Console:
	//0, Less
	//1, Less
```
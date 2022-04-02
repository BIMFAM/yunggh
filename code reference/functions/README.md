# functions

1. <a href="#tag-method">Method</a>
1. <a href="#tag-methodinput">Method Input</a>
1. <a href="#tag-methodlogic">Method Logic</a>

# <a id="tag-method" href="#tag-method">Method</a>
[Microsoft Method documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/methods)

```csharp
	void RunScript()
	{
		ConsoleLog();
	}

	void ConsoleLog()
	{
		Print("Hello World");
	}

	//Console:
	//Hello World
```

# <a id="tag-methodinput" href="#tag-methodinput">Method Input</a>
[Microsoft Method documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/methods)

```csharp
	void RunScript()
	{
		ConsoleLog("Computers Are");
		ConsoleLog("Cool!");
	}

	void ConsoleLog(string print)
	{
		Print(print);
	}

	//Console:
	//Computers Are
	//Cool!
```

# <a id="tag-methodlogic" href="#tag-methodlogic">Method Logic</a>
[Microsoft Method documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/methods)

```csharp
	void RunScript()
	{
		Print(Cube(2));
	Print(Cube(4));
	}

	int Cube(int a)
	{
		return a * a * a;
	}

	//Console:
	//16
	//64
```
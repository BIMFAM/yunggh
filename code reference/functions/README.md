# functions

function

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

function inputs

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

function logic

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
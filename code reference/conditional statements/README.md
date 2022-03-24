# conditional statements

if

```csharp
	bool condition = true;
	
	if (condition)
	{
		Debug.Log("Is True");
	}
	//Console: Is True
```

```csharp
	bool condition = false;

	if (!condition)
	{
		Print("Is True");
	}
	//Console: Is True
```

```csharp
	int i = 0;

	if (i == 0)
	{
		Print("Is True");
	}
	//Console: Is True
```

```csharp
	int i = 1;

	if (i > 0)
	{
		Print("Is True");
	}
	//Console: Is True
```

```csharp
	int i = 1;

	if (i != 1)
	{
		Print("Is True");
	}
	//Console:
```

```csharp
	string text = "Studio";

	if (text == "Studio")
	{
		Print("Is True");
	}
	//Console: Is True
```

if else

```csharp
	string text = "Studio";

	if (text == "Architecture")
	{
		Print("Is True");
	}
	else
	{
		Print("Is False");
	}
	//Console: Is False
```

```csharp
	int i = 0;

	if (i == 0)
	{
		Print("Is True");
	}
	else
	{
		Print("Is False");
	}
	//Console: Is True
```

else if

```csharp
	int i = 0;

	if (i == 0)
	{
		Print("Equal");
	}
	else if (i > 0)
	{
		Print("Greater");
	}
	else
	{
		Print("Less");
	}
	//Console: Equal
```

```csharp
	int i = 1;

	if (i == 0)
	{
		Print("Equal");
	}
	else if (i > 0)
	{
		Print("Greater");
	}
	else
	{
		Print("Less");
	}
	//Console: Greater
```

```csharp
	int i = -1;

	if (i == 0)
	{
		Print("Equal");
	}
	else if (i > 0)
	{
		Print("Greater");
	}
	else
	{
		Print("Less");
	}
	//Console: Less
```

&&

```csharp
	int i = 0;
	string text = "Studio";

	if (i == 0 && text == "Studio" )
	{
		Print("Is True");
	}
	//Console: Is True
```

```csharp
	int i = 0;
	string text = "Studio";

	if (i == 1 && text == "Studio" )
	{
		Print("Is True");
	}
	//Console:
```

||

```csharp
	int i = 0;
	string text = "Studio";

	if (i == 1 || text == "Studio" )
	{
		Print("Is True");
	}
	//Console: Is True
```

```csharp
	int i = 0;
	string text = "Studio";

	if (i == 1 || text == "Architecture" )
	{
		Print("Is True");
	}
	//Console:
```

nested if

```csharp
	int n = 0; int x = 0;
	if(int n == 0)
	{
		if(int x == 0)
		{
			Print("Log");
		}
	}
	//Console:
	//Log
```

```csharp
	int n = 0; int x = 0;
	if(int n == 0)
	{
		Print("First");
		if(int x == 0)
		{
			Print("Second");
		}
	}
	//Console:
	//First
	//Second
```

```csharp
	int n = 0; int x = 1;
	if(int n == 0)
	{
		Print("First");
		if(int x == 0)
		{
			Print("Second");
		}
	}
	//Console:
	//First
```

switch statement

```csharp
	int day = 4;
	switch (day) 
	{
	  case 1:
		Print("Monday");
		break;
	  case 2:
		Print("Tuesday");
		break;
	  case 3:
		Print("Wednesday");
		break;
	  case 4:
		Print("Thursday");
		break;
	  case 5:
		Print("Friday");
		break;
	  case 6:
		Print("Saturday");
		break;
	  case 7:
		Print("Sunday");
		break;
	}
	//Console "Thursday"
```

```csharp
	int a = 20;
	int b = 0

    switch ((a, b))
    {
        case (> 0, > 0) when a == b:
            Print("Both measurements are valid and equal to {a}.");
            break;

        case (> 0, > 0):
            Print("First measurement is {a}, second measurement is {b}.");
            break;

        default:
            Print("One or both measurements are not valid.");
            break;
    }
	//Console "One or both measurements are not valid."
```
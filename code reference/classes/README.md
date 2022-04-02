# classes

1. <a href="#tag-namespace">Namespace</a>
1. <a href="#tag-class">Class</a>
1. <a href="#tag-properties">Properties</a>
1. <a href="#tag-methods">Methods</a>
1. <a href="#tag-constructor">Constructor</a>
1. <a href="#tag-initialize">Initialize</a>
1. <a href="#tag-inheritance">Inheritance</a>

# <a id="tag-namespace" href="#tag-namespace">Namespace</a>
[Microsoft Namespace documentation](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/namespaces)

```csharp
	namespace Agents
	{

	}
```

# <a id="tag-class" href="#tag-class">Class</a>
[Microsoft Class documentation](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes)

```csharp
	namespace Agents
	{
		public class Person
		{

		}
	}
```

# <a id="tag-properties" href="#tag-properties">Properties</a>
[Microsoft Properties documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties)

```csharp
	namespace Agents
	{
		public class Person
		{
			public string Name;
			public int Age;
		}
	}
```

# <a id="tag-methods" href="#tag-methods">Methods</a>
[Microsoft Methods documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/methods)

```csharp
	namespace Agents
	{
		public class Person
		{
			public string Name;
			public int Age;

			public void Talk()
			{
				Print("Talk");
			}
		}
	}
```

# <a id="tag-constructor" href="#tag-constructor">Constructor</a>
[Microsoft Constructor documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors)

```csharp
	namespace Agents
	{
		public class Person
		{
			public string Name;
			public int Age;

			public Person(string name, int age)
			{
				Name = name;
				Age = age;
			}
		}
	}
```

# <a id="tag-initialize href="#tag-initialize">Initialize</a>
[Microsoft Initialize documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/how-to-initialize-objects-by-using-an-object-initializer)

```csharp
	void RunScript()
	{
		Person person = new Person("Smith",27);
		Print(person.Name);
		Print(person.Age);
		person.Talk();
	}

	//Console:
	//Smith
	//27
	//Talk
```

# <a id="tag-inheritance href="#tag-inheritance">Inheritance</a>
[Microsoft Inheritance documentation](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/inheritance)

```csharp
	namespace Agents
	{
		//base class
		public class Person
		{
			public string Name;
			public int Age;

			public Person(string name, int age)
			{
				Name = name;
				Age = age;
			}

			public virtual void Talk()
			{
				Print("Talk");
			}	
		}
		
		//inheritance class
		public class Teacher : Person
		{
			public override void Talk()
			{
				Print("Question");
			}
		}
		
		//inheritance class
		public class Student : Person
		{
			public override void Talk()
			{
				Print("Answer");
			}
		}
	}
```

```csharp
	void RunScript()
	{
		Teacher t = new Teacher ("Alvin",40);
		Student s = new Student ("John",27);
		Person p = new Person("Smith",44);
		t.Talk();
		s.Talk();
		p.Talk();
	}
```

```csharp
	void RunScript()
	{
		Teacher t = new Teacher ("Alvin",40);
		Student s = new Student ("John",27);
		Person p = new Person("Smith",44);
		Person[] persons = new Person[]{t,s,p};
		foreach(Person person in persons){person.Talk();}
	}

	//Console:
	//Question
	//Answer
	//Talk
```
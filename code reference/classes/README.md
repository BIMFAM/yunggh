# classes

Namespace

```csharp
	namespace Agents
	{

	}
```

Class

```csharp
	namespace Agents
	{
		public class Person
		{

		}
	}
```

Properties

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

Methods

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

Constructor

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

Instantiate

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

Inheritance

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
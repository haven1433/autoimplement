# Goal
Help .net developers to use good design patterns by reducing the amount of code they need to write in order to leverage those design patterns.

# Demo
Here's a quick look at what your code could look like when using autoimplement:

In an assembly, you have the following interface:

```
public interface IRecord
{
   int StoredValue { get; set; }
   void Synchronize();
   bool IsFrom(string system);
}
```

## Calling autoimplement

`AutoImplement <yourassemblylocation> IRecord`

AutoImplement will then automatically implement the interface three different ways, creating a Stub, a Composite, and a Decorator.

## Stub

```
var record = new StubRecord();
record.Synchronize = () => record.StoredValue = 12;
RunTest(record);
```

## Composite
```
var record = new CompositeRecord();
record.Add(new LocalRecord("foo"));
record.Add(new LocalRecord("bar"));
var result = record.IsFrom("localhost");
```

## Decorator
```
public class EnhancedRecord : RecordDecorator
{
   public EnhancedRecord(IRecord core) => base.Core = core;
   
   public override void Synchronize()
   {
      Console.WriteLine("Synchronizing...");
      base.Synchronize();
   }
}
```

# A Quote
*Design Patterns are missing language features.*

I don't know who this quote is attributed to, and it may not be entirely true. But you need only look at python's convention of prefixing functions with underscores to make them "private" to understand that there is some truth to it. Likewise, before C++ was created, developers would use design patterns to make "objects" in C.

Many such design patterns have become so common and used so frequently that they have evolved into language features and framework components. For example, the Iterator Pattern now has built-in language support in languages like C# and Java, and C# formalizes the Command Pattern through the `System.Windows.Input.ICommand` interface. Lazy instantiation is supported through `System.Lazy`, and the Prototype Pattern is supported through `System.Object.MemberwiseClone`. The framework goes so far as to provide default useful implementations of `Equals` and `GetHashCode` for value types.

There are many other design patterns that the framework and language provide no support for at all. Some of them, like the Adapter and Builder Patterns, share little resemblance each time you see them. Other patterns are almost identical every time. The intention of this utility is to provide implementations of those simpler patterns through code generation. Ideally I would write an entirely new language that included these patterns as features. However, since I am not a language designer, I lean on code generation as a cruch and hope that other developers can add these to .net languages in the future.

# The Patterns

The three patterns simple enough to allow for automatic implementation are: Stub, Decorator, and Composite. Each will be discussed in detail below.



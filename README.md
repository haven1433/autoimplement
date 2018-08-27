# Goal
Help .net developers to use good design patterns by reducing the amount of code they need to write in order to leverage those design patterns.

# Demo
Here's a quick look at what your code could look like when using autoimplement:

In an assembly, you have the following interface:

```
public interface IRecord {
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
var record = new StubRecord { StoredValue = 7 };
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
   
   public override void Synchronize() {
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


## Stub

Stubs are most often seen used as "Test Doubles", but they can be useful for normal code as well.

AutoImplement creates Stubs with a name that matches the interface, but the leading 'I' is replaced with 'Stub'. So a Stub of `ICommand` would be called `StubCommand` and placed in the same namespace.

In general, you use a Stub to replace a full object when the full object is not required or not desirable. You could use a Stub as a default value so as to avoid NullPointerExceptions, or give a Stub a basic fake implementation when testing a unit of code, or use a Stub as a way to create a customized instance of an interface when making a whole new type feels heavyweight.

### Example 1: ICommand

Consider `System.Windows.Input.ICommand`. WPF uses commands regularily, and provids a special implementation of ICommand called `RoutedCommand` that it uses heavily. But despite this specialized implementation, most WPF types are designed to accept any implementation of `ICommand`.

One possible use of `ICommand` is to provide an implementation for a `Button`. When the `Button` is clicked, it can either call a `Clicked` event handler (usually implemented inside a UserControl that ownes the `Button`), or it can call the `Execute` method on an `ICommand` (usually part of a ViewModel, allowing for a better separation between the UI and the business logic). `ICommand.CanExecute` gets called to check if the `Button` should even be enabled, and the `ICommand` provides an event handler called `CanExecuteChanged` that the Button will automatically listen to in order to determine when to check `CanExecute` again.

Overall, this is all very well designed. However, if your application needs many commands, you'll need to create a new implementation of `ICommand` for every command you need. This can cause an explosion of types in your assembly, or worse, it can cause extremely complex dependency scenarios if several of your commands use the same data.

At some point, you decide to make a reusabe Command, one that accepts delegates in the constructor or as properties, and then calls those delegates as implementations for `CanExecute` and `Execute`. It probably also has a method called `RaiseCanExecuteChanged` to allow the owner of the object to raise the event when needed. Maybe you name this class `DelegateCommand` or `RelayCommand`. Searching the internet will give you exactly these names, along with implementations of other people who felt this was useful enough to share.

Congratulations! You've just created a Stub. `IDisposable` and `IComparer<>` are other common .Net interfaces where developers commonly want a Stub in real code. You likely have small interfaces of your own, or small interfaces in common frameworks or tools that you use, and having automatic Stubs for these can help keep related code close together.


### Example 2: Null Objects

The newer version of C# include the '?.' operator, useful for simplifying code where the object you're using may be null. However, how much simpler could your code be if you just never had to deal with `null`? Tony Hoare, the inventor of ALGOL W, called the null reference his 'billion dollar mistake.'

Here's code written before C#6:
```
if (source != null) {
   return source.CalculateResult();
} else {
   return -1;
}
```

Here's code written using C#6:

```
return source?.CalculateResult() ?? -1;
```

You probably think this looks much better. But wait, what if we had a special version of `source` available that encoded that extra bit of information about how the application should behave if there is no source object? This is usually called a NullObject, and it makes your code look like this:

```
return source.CalculateResult();
```

This is clearly the cleanest of the options, especially if there are other places where you have to do similar operations of conditionally returning the -1. Below is the setup for a stub object that can act as a NullObject in this case:

```
source = new StubSource { CalculateResult = () => -1 };
```

In this way, you can cross "make null implementation" off your todo list as well: the Stub implementation functions out-of-the-box by returning default values from every method and property, and can be quickly customized to fit whatever custom scenario you need.

### Example 3: Testing

You may have heard of Moq, or NSubstitute, or FakeItEasy. Each of these frameworks exist to assist in the development of Unit Tests by allowing you to easily replace real dependencies with fake ones, allowing you to test specific units of code in isolation, independent of the behavior of the rest of your application. These can be very helpful, but come at a slight cost:

* Some frameworks require extension methods on basically everything, which can greatly reduce the helpfulness of Intellisense.
* Setting up a custom implementation can be quite verbose.
* Use of dynamic proxies make them unsuitable for use in non-testing scenarios.

Because of this, Microsoft decided to produce their own alternative solution, called **Microsoft Fakes**. Fakes completely removes the first two issues I listed above by actually generating special implementations for each interface, instead of doing custom compilation at runtime. Sound familiar? Unfortunately, the assemblies that Microsoft generates once again make the implementation only suitable for testing.

The Stubs created by AutoImplement have a lot in common with the Stubs from Microsoft Fakes, and can be used in many of the same scenarios. However, because AutoImplement generates individual implementations as source code files, you can use the Stub in whatever way you see fit, whether for testing, a NullObject, or as an extensible starting point for small objects.

### Full Details

In order to afford the most succinct and obvious usage, AutoImplement uses Explicit Interface Implementation in the Stubs it generates.

Normally when implementing an interface in C#, the compiler does the work of figuring out which of the class members are supposed to be attached to each interface member by doing a simple matching on the member name (and parameters, in the case of methods). However, C# also allows interfaces to be implemented 'explicitly', such as the following:

```
public class MyDisposable : IDisposable {
   void IDisposable.Dispose() { ... }
}
```

This creates a method with a special name that can only be accessed by casting to the interface type. This is normally a disadvantage, but it has one very important advantage that comes with it: it allows the type to have a *different* member with the same name. AutoImplement takes advantage of this feature by creating members that can behave very similarily to the interface members, but with some extra functionality.

Throughout the rest of this section, we'll share the implementation details for the Stubs various types of members. In each case, calling the interface members just forwards to the Stub's members.

### Methods are replaced with Delegate Properties

Consider the `IDisposable` interface. If you made a stub of it, the `Dispose` member in the `StubDisposable` type would be a public property of type `System.Action`. This lets you 'call the method' with the usual syntax, but it also lets you use get and set operations to change what the method does.

Interfaces are allowed to have multiple methods with the same name, given that their parameters are different. Since types cannot have multiple properties with the same name, a sanitized list of parameters is appended to the end of the property if there is already a property with that name. So for the following interface:

```
public interface IMaxFinder {
   int Max(int a, int b);
   double Max(double a, double b);
}
```

the first delegate property of `StubMaxFinder` would be name `Max`, while the second would be named `Max_double_double`.

.Net provides the `Action<>` and `Func<>` delegate types, and AutoImplement uses those where possible. However, if a method has `out` or `ref` parameters, AutoImplement must create a custom delegate for the method. If this is the case, then the custom delegate type will be named based on the method name and the parameter types, ignoring the out/ref modifiers since you cannot create two methods with the same signature with only the modifiers changed. For example, a method like `bool TryThing(string input, out IDisposable result)` in an interface would result in a delegate `public bool TryThingDelegate_string_System_IDisposable(string input, out IDisposable result)`. The delegate would be placed inside the Stub class and used by a property named `TryThing_string_System_IDisposable`. The name of this delegate will almost never be important, but it's there if you need to cast to it. More likey, you'll only need to know the name of the property, which always has the parameters types appended in the case of methods with `ref` or `out` parameters.

### Properties are replaced with Fields of type PropertyImplementation

AutoImplement ships with an additional assembly called `System.Delegation`. It includes two types, `System.Delegation.PropertyImplemenation` and `System.Delegation.EventImplementation` which will be described below. The simplest way to think about them is as an object that wraps up the details of properties and events in an easy to edit way. Here's an example of some ways to use `PropertyImplemention`:

```
var number = new PropertyImplementation<int>();
int result = number;
number = 1;
number.value = 2;
number.get = () => 3;
number.set = value => result = value;
```

Normally you'll never have to create your own `PropertyImplementation` objects. Instead, AutoImplement will create public fields of the appropriate PropertyImplementation generic type to represent all the properties in the interface. Here's a short description of what's going on in the example above.

* number's initial value is `default(int)` (0), so result is 0 thanks to an implicit cast inside `PropertyImplementation`.
* `number = 1` uses an implicit cast inside `PropertyImplementation` to create a new `PropertyImplementation` object who's initial value is 1.
* `number.value` accesses a public field within the `PropertyImplementation`, allowing you to get and set the value inside the property directly, bypassing the accessors.
* `number.get` and `number.set` are fields to delegates, allowing you to customize the behavior of those methods in the property. By default, the just get and set the property's `value` field.

When used by AutoImplement, the interface get or set calls will call into the `PropertyImplementation`'s `get` and `set` members. So you might use `StubList` (an implementation of `IList` by calling `stub.Count = 7` or `stub.Count = () => myList.Count`, depending on the situation.

Special mention goes to the `Item` property, the only property in C# that is allowed to have arguments, which is exposed as an indexer, such as `this["bob"]`. Since this property can have multiple values, a `PropertyImplementation` is not used for it. Instead, AutoImplement exposes it via a pair of methods called `get_Item` and `set_Item` which are used for accessing and setting the property.

### Events are replaced with Fields of type EventImplementation

Events in AutoImplement work very similar to Properties. They're exposed using a public field with a name that matches the member of the interface. However, instead of providing `get`, `set`, and `value`, `System.Delegation.EventImplementation` provides public fields for `add`, `remove`, and `handlers`. It also provides + and - operators and an Invoke method, allowing the EventImplementation object to be used like the original event.

```
var stub = StubNotifyPropertyChanged();
stub.PropertyChanged += (sender, e) => DoAThing(); // the default add handler is called, adding the lambda expression to the list of handlers.
int count = stub.PropertyChanged.handlers.Count;
stub.PropertyChanged.add = value => count += 1;    // the default add handler is replaced
stub.PropertyChanged += (sender, e) => DoAnotherThing(); // DoAnotherThing is not added to the list of handlers, because 'add' now does something custom instead.
stub.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SomeProperty"));
```

As mentioned, custom classes found online such as DelegateCommand or RelayCommand might have a special method called "RaiseCanExecuteChnaged" that has the same behavior as `EventImplemenation`'s `Invoke` method. Invoke was chosen to help make the stub's member behave closer to how delegates behave in the rest of .Net.

## Decorator

Let's say you have an object that works the way you want. Now you decide that you want some version of that object to behave slightly differently, usually by adding some extra behavior but without adding extra methods. You might make a subclass that adds the extra functionality. But this has a few disadvantages.

* Now, in order to switch behaviors, you must completely change objects, potentially losing state. This usually locks you into a situation where changing objects while running is impossible, so at least part of the application has to be torn down and brought back up in order to use the new functionality.
* Acting as a subclass can be problematic if the type you're wanting to extend already has several children. You would need to create a new child for every type of subclass.
* If the basetype has several parameters, each of those parameters must also be passed to the new child type, simply so that the child can forward them to the base type. This can cause unnecessary coupling, and in the worst cases, massive code duplication.

To get around this, we can build a type that uses the "has-a" relation instead of the "is-a" relation. That is, instead of being a child of the original type, *contain* the original type and implement the same interface. This provides full control over the original object and full access to its original functionality without any of the problems listed above. The implementation can be changed whenever you want without losing state just by wrapping and replacing the original object, and if multiple children extend the base type, the decorator can equally be applied to any of them with no code duplication. However, it comes with its own challenge: creating a whole new type that implements the same (possible large) interface just to add a few bits of custom behavior can involve writing quite a bit of glue code.

That's where AutoImplement comes in. AutoImplement can provide automatic decorator implementations of interfaces, where every method is virtual and a protected property exposes the wrapped object (naming it "Inner*Thing*", for whatever the interface name is). You can then extend this base Decorator type to override only the methods you want, and expose the wrapped object in the way that you choose, whether through a constructor, public property, or not at all.

### Example 1: Logging

One example that's often used in Aspect Oriented programming is a Logging feature. Often, adding logging doesn't mean adding any new members. But it does mean changing several existing members. This sort of cross-cutting concern can be solved in languages without aspects by imploying Decorators.

For example, I might want the ability to log whenever a disposable goes out of scope. After running `AutoImplement <mscorelib GAC full name> IDisposable` and pulling in the generated Decorator, I could write the following:

```
public class DisposeLogger : DisposableDecorator {
   public DisposeLogger(IDisposable disposable) => InnerDisposable = disposable;
   public override void Dispose() {
      Console.WriteLine($"Calling Dispose on {InnerDisposable.ToString()}...");
      base.Dispose();
   }
}
```

This is a silly example, because `IDisposable` provides only a single method, so writing a decorator without using `DisposableDecorator` is trivial. The only shortcut you gain from using the base class is not having to declare a field for your `InnerDisposable`. However, the more members an interface has, the greater the benefit of having the base decorator class handle all the glue code for you. 

### Example 2: Drawing Visuals

Consider you've written an interface called `IShape` with a method called `Draw` that is supposed to Draw the object to some surface. You have implementations that draw circels, rectangles, and other geometric shapes. The interfaces contains other properties to specify the width, height, and location of the object, and possibly some methods to move the object around.

Now you decide that you want the ability to put a simple border around any of those shapes. Then you could use AutoImplement to create a `ShapeDecorator` and extend it to override only the `Draw` method, leaving everything else the same. Now you have a simple, composable object, that lets you put your border around any of the shapes, without having to implement a child class for each shape, or having to come up with a new interface. Your `BorderedShape` behaves just like any other `IShape`, because it is one... just without having to give all the details itself.

### Full Details

### protected IThing InnerThing

Each Decorator created by AutoImplement provides a protected property named similarily to the interface. For example, running AutoImplement on `ICommand` would create a decorator type named `CommandDecorator` with a protected property of type `ICommand` named `InnerCommand`. All the virtual methods in the generated decorator just forward to that `InnnerCommand` if it's not null.

### virtual methods

Every method, property, and event in the interface is given a virtual implementation that calles the Inner*Thing*. You can override it to provide custom behavior, or leave it alone with confidence that the Inner*Thing*'s implementation will be run.

The exception to this is if the interface implements another interface with a conflicting member. For example, `IEnumerable<>` implements `IEnumerable`, which means that an `EnumerableDecorator` would need to implement both `GetEnumerator()` and `GetEnumerator()`. Since these members are only distinguished by their return value, explicit interface implementation must be used for at least one... and explicit implementations cannot be virtual. Because of this restriction, AutoImplement will create a virtual implementation for the most derived version, and have all less derived versions call the more derived version. This will usually be correct. In the case that it is not, it's still possible to create an explicit interface implementation in your leaf class that provides different behavior for the less-derived version of the method. But if you find yourself in that situation, you may consider rethinking your interface inheritance chain.

### if the inner object is null

You've noticed by now that AutoImplement's decorators don't provide constructors that take in the Inner*Thing*. This is to maximize the number of ways you can choose to expose or restrict the Inner*Thing* access. However, that also means that if you don't explicitly set the value of Inner*Thing* at some point, then your decorator will be acting on null.

Each of the decorator methods is written similar to the following:

```
public virtual int DoThing(int value) {
   if (InnerThing != null) {
      return InnerThing.DoThing(value);
   } else {
      return default(int);
   }
}
```

In the case of out parameters, the out parameters will also return default values. While this is not perfect, doing nothing and returning default is the most sensible thing the Decorator could do besides throwing an exception. By choosing not to throw an exception, it becomes possible to use an Decorator with no Inner*Thing* as an additional way to implement default base objects and null objects.

## Composite

A Composite is an object that treats many objects as a single object. This can reduce the complexity of tasks when working with lists of elements.

When you give AutoImplement an interface, it generates a composite that:

(1) Extends from `List<T>`, where `T` is your interface
(2) Implements your interface

Calling the list methods allows the Composite to act like a collection of implementations, while calling the interface methods allows the Composite to act like a single implementation. Each of the interface members is implemented using a for loop that simply forwards the arguments to each element in the list. If the member has a return value, the composite returns a useful value only if every element agrees on what value to return. Otherwise, it returns `default`.

### Example 1: Observers

C# provides events for callbacks. An object can notify other objects of something happening (or ask them to take some unknown action) by calling an event. However, if a type has many of these notifications that it wants to perform, sometimes it makes more sense to package them up into an observer interface. Instead of having many events where a subscriber can pick and choose which events they want to listen to, implementing an observer interface means that you'll get notifications for every method in the observer. This can be a handy way to make sure that the developer using your type is aware of everything your type tries to notify on: they can't miss it, because if they do, the code will not compile.

```
public interface IStateObserver {
   void GameStarted(GameToken token);
   void GamePaused(GameToken token);
   void GameWon(GameToken token);
   void GameLost(GameToken token);
   void GameTied(GameToken token);
}
```

However, events allow for multiple subscribers. To get a similar interaction with observers, you don't just want one observer: you want a list of observers. But this leads to loops every time you want to interact with the observers, where you're calling the same methods with the same parameters, but with a different observer each iteration. This can gum up your code and make it harder to understand.

The answer is to use a CompositeObserver. The Composite acts like a single element, but contains a list of the observers. Since the composite is also a list, it automatically includes methods for adding, removing, and reordering the observers. To the outside world, you can act as if you own a list of observers, while within the class, you can interact with the entire group as if it is just a single observer.

```
public void AcceptInput(Key key) {
  ...
  if (key == Keys.Space) {
     PauseGameLogic();
     this.compositeObserver.GamePaused(this);
  }
  ...
}
```

### Example 2: Multiselect

Suppose you have a banking application. Your application allows you to select an account, and then show all the details of that account: the owner, the account number, the balance, when it was opened, etc.

Now suppose you want to implement multiselect to allow for quick editing. Perhaps you want to quickly deposit $10 into 4 accounts, or run the inactivity fee routine to deduct a from multiple balances, or fix a typo in every account owned by "Mr. John Smiht".

Adding the multiselect could be lot of work, since it could include the creation of entirely new workflows. But if you could select multiple accounts and then make them all act like a single account, showing shared data and operating as a single transaction, then no new workflows would be needed... just a Composite.

### If the return values don't match

Unfortunately, implementing the interface exactly doesn't allow for any sort of option type to specify that the results of methods didn't match. For this reason, it's difficult to interpret the results of numeric value types return from composites. Is it returning '0' because that's the right answer, or because some of the results didn't match?

However, this is the most reasonable possible behavior for many situations. For bools, you can think of the Composite and returning the and (&&) of all the results. If all are true, it returns true; otherwise false. For reference types, `null` is a totally reasonable 'didn't match' result. So this system is only really a problem for bytes, shorts, ints, longs, floats, and doubles. I'd recommend that if you need reasonable results for those (perhaps for a multiselect operation), you define your interface to used nullable values, such as `int?` and `double?`. This provides a reasonable return value in the case of non-matching.

### If the composite is empty

An empty composite returns values much like a composite who's elements don't agree on what the return value should be: the composite just returns the default value. This means that an empty Composite implementation is yet another reasonable implementation of the NullObject pattern. One would wonder why anyone would ever bother writing an explicit NullObject in C#, when the degenerate case of so many other design patterns already cover its applications so handily.

### Custom operations on the collection

You may find yourself wanting to aggregate results in some other way for specific method calls. The Composite implementation does not do anything special to allow this, but it also doesn't prevent it. In places where custom operations are desired, you can take advantage of the Composite being a child of `List<>` and use Linq to perform your operation.

```
bool resultsCombinedFromAnd = composite.CanEdit;
bool resultsCombinedFromOr  = composite.Any(element => element.CanEdit);
```

It's more verbose than extra operations in the Composite type, but the simplification makes the Composite type more predictable and therefore easier to learn.

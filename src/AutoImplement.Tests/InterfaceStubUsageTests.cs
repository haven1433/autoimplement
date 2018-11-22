using HavenSoft.AutoImplement.Tests.Types;
using System;
using System.Linq;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   /// <summary>
   /// This class exists to show example usage of the various things AutoImplement can do.
   ///
   /// Each test shows you two views of the same object.
   /// (1) How the stub appears to you as you set it up.
   /// (2) How the stub appears to the application that's using it as the interface.
   /// </summary>
   public class InterfaceStubUsageTests {
      /// <summary>
      /// The assignment operator can handle most simple scenarios for properties. Just assign the value you want the property to have.
      /// </summary>
      [Fact]
      public void SimplePropertyUsage() {
         var stub = new StubExample();
         IExample example = stub;

         // you can use the property the same whether the interface has a getter, a setter, or both.
         stub.PropertyWithGetter = 12;
         stub.PropertyWithGetAndSet = 3.0;

         // from the interface, everything works as expected
         Assert.Equal(12, example.PropertyWithGetter);
         Assert.Equal(3.0, example.PropertyWithGetAndSet);
      }

      /// <summary>
      /// You can access the getter and change the implementation at will.
      /// </summary>
      [Fact]
      public void AdvancedPropertyGetterUsage() {
         var stub = new StubExample();
         IExample example = stub;


         // you can use ".get" to assign a custom get accessor
         int callCount = 0;
         stub.PropertyWithGetter.get = () => { callCount++; return 12; };

         Assert.Equal(12, example.PropertyWithGetter);
         Assert.Equal(1, callCount);


         // you can get the value from the default setter using ".value"
         stub.PropertyWithGetAndSet.get = () => stub.PropertyWithGetAndSet.value + 1;

         example.PropertyWithGetAndSet = 3.8;
         Assert.Equal(4.8, example.PropertyWithGetAndSet);
      }

      /// <summary>
      /// You can access the setter and change the implementation at will.
      /// </summary>
      [Fact]
      public void AdvancedPropertySetterUsage() {
         var stub = new StubExample();
         IExample example = stub;


         // you can use ".set" to assign a custom set accessor
         int callCount = 0;
         stub.PropertyWithSetter.set = value => { callCount++; };

         example.PropertyWithSetter = true;
         Assert.Equal(1, callCount);


         // you can set the value for the default getter using ".value"
         stub.PropertyWithGetAndSet.set = value => stub.PropertyWithGetAndSet.value = value + 1;

         example.PropertyWithGetAndSet = 3.8;
         Assert.Equal(4.8, example.PropertyWithGetAndSet);
      }

      /// <summary>
      /// You can access get_Item and set_Item to change the implementation of this[arg].
      /// </summary>
      [Fact]
      public void IndexerPropertyUsage() {
         var stub = new StubExample();
         IExample example = stub;

         // since the indexer property has arguments, it's exposed as get_Item and set_Item separately.
         stub.get_Item = index => { return "No Value"; };

         // note that set_Item has one additional parameter: the value being set
         stub.set_Item = (index, value) => { /* put useful code here */ };

         Assert.Equal("No Value", example[12]);

         // note that for normal properties, there is a default backing storage (.value) included.
         // but for index properties, there is no such default storage.
         // if you want your stub to be able to share data between get_Item and set_Item, you have to designate everything yourself.
      }

      /// <summary>
      /// You can use +=, -=, and Invoke on event handlers to handle most simple scenarios.
      /// </summary>
      [Fact]
      public void SimpleEventUsage() {
         var stub = new StubExample();
         IExample example = stub;

         // you can add and remove handlers like normal
         int count = 0;
         stub.SimpleEvent += (sender, e) => count++;

         // you can invoke the event like you would from within the stub
         stub.SimpleEvent.Invoke(stub, EventArgs.Empty);
         Assert.Equal(1, count);

         // you can access all the handlers that have been added.
         Assert.Single(stub.SimpleEvent.handlers);
      }

      /// <summary>
      /// You can access the adder and change the implementation at will
      /// </summary>
      [Fact]
      public void AdvancedEventAdd() {
         var stub = new StubExample();
         IExample example = stub;

         // you can use ".handlers" to modify the list of registered handlers
         int count = 0;
         stub.SimpleEvent.add = handler => { stub.SimpleEvent.handlers.Add(handler); count++; };

         example.SimpleEvent += (sender, e) => { };
         Assert.Equal(1, count);
      }

      /// <summary>
      /// You can access the remover and change the implementation at will
      /// </summary>
      [Fact]
      public void AdvancedEventRemove() {
         var stub = new StubExample();
         IExample example = stub;

         // you can use ".handlers" to modify the list of registered handlers
         int count = 0;
         stub.SimpleEvent.remove = handler => { stub.SimpleEvent.handlers.Remove(handler); count++; };

         void Handler(object sender, EventArgs e) { }
         example.SimpleEvent -= Handler; // just like with standard events, removing something that isn't there doesn't error.
         Assert.Equal(1, count);
      }

      /// <summary>
      /// AutoImplement uses EventHandler<> for everything.
      /// </summary>
      [Fact]
      public void EventHandlerArgsComparison() {
         var stub = new StubExample();
         IExample example = stub;

         // for generic events, ".handlers" is a collection of EventHandler<T>.
         EventHandler<AssemblyLoadEventArgs> handler1 = stub.GenericEvent.handlers.FirstOrDefault();

         // for custom event classes, ".handlers" is _still_ a collection of EventHandler<T>.
         EventHandler<UnhandledExceptionEventArgs> handler2 = stub.SpecificEvent.handlers.FirstOrDefault();

         // even simple EventHandler objects use a collection of EventHandler<T>.
         EventHandler<EventArgs> handler3 = stub.SimpleEvent.handlers.FirstOrDefault();

         // internally, AutoImplement does the conversion to the appropriate eventhandler type used by the interface.
         // the biggest limitation here is that any event implemented by auto-implement:
         //     - the event must have exactly two parameters
         //     - the first parameter must be of type object.
      }

      /// <summary>
      /// Assign custom behavior to most methods by using the assignment operator.
      /// </summary>
      [Fact]
      public void SimpleMethodUsageStub() {
         var stub = new StubExample();
         IExample example = stub;

         // setup a method by assigning a function to that method
         stub.VoidMethod = () => { };

         // you can even call the method the normal way from the stub.
         stub.VoidMethod();

         // methods that return something are setup in the same way
         stub.StructMethod = () => 7;
         var result = stub.StructMethod();

         Assert.Equal(7, result);
         Assert.Equal(7, example.StructMethod());
      }

      /// <summary>
      /// Generic methods are more tricky.
      /// In .Net, a "generic delegate" refers to a parameterized delegate.
      /// There's no concept of a single delegate that provides multiple versions based on a generic parameter.
      /// So assigning implementations to a generic method works on a per-generic-parameter basis.
      /// </summary>
      [Fact]
      public void GenericMethodUsage() {
         var stub = new StubExample();
         IExample example = stub;

         stub.ImplementGenericMethod<int>(() => 5);
         stub.ImplementGenericMethod<string>(() => "bob");

         Assert.Equal(5, example.GenericMethod<int>());
         Assert.Equal("bob", example.GenericMethod<string>());

         // if no implementation is assigned, default values are used when needed.
         Assert.Equal(default(double), example.GenericMethod<double>());
         Assert.Equal(default(bool), example.GenericMethod<bool>());

         // if you want to make a more complete implementation that works for any generic parameter,
         // put your custom implementation in a decorator and then wrap the stub with that decorator.
         // This DOES require making a custom class, but there's no other way to make a generic-parameterized method:
         // you can't use a lambda or pass in a delegate.
      }

      /// <summary>
      /// out and ref methods are only slightly more verbose than normal methods.
      /// </summary>
      [Fact]
      public void OutAndRefMethodUsage() {
         var stub = new StubExample();
         IExample example = stub;

         // IExample has an out method named "OutMethod(int, out int)"
         // in the stub, methods with out or ref parameters are exposed using the method name suffixed with the parameter types.
         // also, lambas using out/ref parameters require full types specificied for the parameters.
         stub.OutMethod_int_int = (int input, out int output) => { output = input + 2; };

         int number;
         example.OutMethod(5, out number);
         Assert.Equal(7, number);

         // ref params work the same way.
         stub.RefMethod_int_string = (int input, ref string reference) => true;
      }
   }
}

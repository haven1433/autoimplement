using HavenSoft.AutoImplement.Tests.Types;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   /// <summary>
   /// This class exists to show example usage of the various things AutoImplement can do.
   ///
   /// Each test shows you two views of the same object.
   /// (1) How the stub appears to you as you set it up.
   /// (2) How the stub appears to the application that's using it as the interface.
   /// </summary>
   public class ClassStubUsageTests {
      /// <summary>
      /// Use DeferConstruction to provide an implementation _before_ the base class constructor is called.
      /// </summary>
      [Fact]
      public void DeferConstructionUsage() {
         PoorlyImplementedAbstractClass baseClass;
         StubPoorlyImplementedAbstractClass stub;
         using (StubPoorlyImplementedAbstractClass.DeferConstruction(7, out stub)) {
            stub.ConfigureSomething = () => "race car";
         }

         baseClass = stub;
         Assert.Equal("rac ecar", baseClass.ReversedConfiguration);
      }

      /// <summary>
      /// MethodToCall2 is protected on ExampleClass.
      /// But on the stub, protected methods are made public.
      /// Even though you can't override it, you can still call it.
      /// </summary>
      [Fact]
      public void ProtectedMethodUsage() {
         var stub = new StubExampleClass();
         ExampleClass example = stub;

         stub.MethodToCall2();

         Assert.Equal(1, example.CallsToMethod);
      }

      /// <summary>
      /// sampleField is protected on ExampleClass.
      /// But on the stub, protected fields are exposed through public properties.
      /// Even though you can't override it, you can get and set it.
      /// </summary>
      [Fact]
      public void ProtectedFieldUsage() {
         var stub = new StubExampleClass();

         stub.sampleField = 7;

         Assert.Equal(7, stub.sampleField);
      }

      /// <summary>
      /// Every constructor in the base class, whether public or protected, gets an overload in the stub.
      /// The overload constructor calls the appropriate base class constructor.
      /// </summary>
      [Fact]
      public void CanCallOverloadConstructor() {
         var stub = new StubExampleClass(3);
         ExampleClass example = stub;

         Assert.Equal(3, example.CallsToMethod);
      }

      /// <summary>
      /// If a constructor requires specific types, you don't have to match those types exactly.
      /// You can use subtypes.
      /// This is true for both Construction and DeferConstruction.
      /// </summary>
      [Fact]
      public void CanCallConstructorWithSubclassParameters() {
         var stub1 = new StubExampleClass("bob");

         StubExampleClass stub2;
         using (StubExampleClass.DeferConstruction("tommy", out stub2)) { }

         ExampleClass example = stub1;
         Assert.Equal(3, example.CallsToMethod); // length of 'bob'

         example = stub2;
         Assert.Equal(5, example.CallsToMethod); // length of 'tommy'
      }
   }
}

using HavenSoft.AutoImplement.Tests.Types;
using System;
using System.ComponentModel;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   public class FunctionalTests {
      [Fact]
      public void CustomEventStubClassActuallyWorksAsExpect() {
         void HelperMethod(object sender, PropertyChangedEventArgs args) { }
         var implementation = new StubNotifyPropertyChanged();
         implementation.PropertyChanged += HelperMethod;
         implementation.PropertyChanged -= HelperMethod;
         Assert.Empty(implementation.PropertyChanged.handlers);
      }

      // this test shows that problems exist in the PoorlyImplementedAbstractClass that make it impossible to create a Stub the normal way.
      // it isn't actually a test of the framework, but a verification that the next test (CanDeferConstruction) is actually fixing a real issue.
      [Fact]
      public void TryingToCreateAStubFromThePoorlyImplementedAbstractClassNormallyFails() {
         Assert.Throws<ArgumentNullException>(() => {
            var stub = new StubPoorlyImplementedAbstractClass(7);
         });
      }


      [Fact]
      public void CanDeferConstruction() {
         StubPoorlyImplementedAbstractClass stub;
         using (StubPoorlyImplementedAbstractClass.DeferConstruction(7, out stub)) {
            stub.ConfigureSomething = () => "race car";
         }

         Assert.Equal("rac ecar", stub.ReversedConfiguration);
      }
   }
}

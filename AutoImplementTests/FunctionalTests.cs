using Xunit;

namespace AutoImplementTests {
   public class FunctionalTests {
      [Fact]
      public void CustomEventStubClassActuallyWorksAsExpect() {
         void HelperMethod(object sender, int args) { }
         var implementation = new StubHasCustomEvent();
         implementation.CustomEvent += HelperMethod;
         implementation.CustomEvent -= HelperMethod;
         Assert.Empty(implementation.CustomEvent.handlers);
      }
   }
}

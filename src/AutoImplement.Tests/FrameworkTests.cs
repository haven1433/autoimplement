using HavenSoft.AutoImplement.Tests.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Delegation;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   /// <summary>
   /// These tests verifies assumptions that AutoImplement makes about how .Net behaves in specific situations.
   /// They basically verify that AutoImplement's automatic implementations can actually work.
   /// </summary>
   public class FrameworkTests {
      /// <summary>
      /// This test verifies that 'EventHandler<PropertyChangedEventArgs>' and 'PropertyChangedEventHandler' are interchangable.
      /// </summary>
      [Fact]
      public void CustomEventStubClassActuallyWorksAsExpect() {
         void HelperMethod(object sender, PropertyChangedEventArgs args) { }
         var implementation = new StubNotifyPropertyChanged();
         implementation.PropertyChanged += HelperMethod;
         implementation.PropertyChanged -= HelperMethod;
         Assert.Empty(implementation.PropertyChanged.handlers);
      }

      /// <summary>
      /// this test shows that problems exist in the PoorlyImplementedAbstractClass that make it impossible to create a Stub the normal way.
      /// it isn't actually a test of the framework, but a verification that DeferConstructionUsage is actually fixing a real issue.
      /// </summary>
      [Fact]
      public void TryingToCreateAStubFromThePoorlyImplementedAbstractClassNormallyFails() {
         Assert.Throws<ArgumentNullException>(() => {
            var stub = new StubPoorlyImplementedAbstractClass(7);
         });
      }

      /// <summary>
      /// AutoImplement's implementation of generic methods requires a way to express different implementations for each set of generic arguments
      /// So the generic arguments are put into a type array, and that array is used as a key into a dictionary.
      /// This test proves that dictionaries can handle that comparison correctly.
      /// </summary>
      [Fact]
      public void DictionaryCanUseTypeArrayAsKey() {
         // note that this test WILL FAIL if you use the default equality comparerer.
         var dict = new Dictionary<Type[], string>(new EnumerableEqualityComparer<Type>());

         dict.Add(new[] { typeof(int) }, "int");
         dict.Add(new[] { typeof(string) }, "string");

         Assert.Equal(2, dict.Count);
         Assert.True(dict.ContainsKey(new[] { typeof(int) }));
         Assert.Equal("int", dict[new[] { typeof(int) }]);
      }
   }
}

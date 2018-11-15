using System;
using System.Collections.Generic;

namespace HavenSoft.AutoImplement.Tests.Types {
   /// <summary>
   /// This interface is used by the tests to verify that implementations work as expected.
   /// Each member exists only to show how that member is able to be used.
   /// </summary>
   public interface IExample {

      int PropertyWithGetter { get; }
      bool PropertyWithSetter { set; }
      double PropertyWithGetAndSet { get; set; }

      string this[int index] { get; set; }

      event EventHandler SimpleEvent;
      event EventHandler<AssemblyLoadEventArgs> GenericEvent;
      event UnhandledExceptionEventHandler SpecificEvent;

      void VoidMethod();
      IEnumerable<string> ObjectMethod();
      int StructMethod();
      T GenericMethod<T>();

      void OutMethod(int input, out int output);
      bool RefMethod(int input, ref string reference);

   }
}

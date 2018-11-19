namespace HavenSoft.AutoImplement.Tests.Types {
   /// <summary>
   /// This class is used by the tests to verify that implementations work as expected.
   /// Each member exists only to show how that member is able to be used.
   /// </summary>
   public abstract class ExampleClass {

      // it should be possible to access this from stub
      // so it gets wrapped in a public property with the same name.
      protected int sampleField;

      // this member should be left totally alone, since it is public and non-virtual.
      public int CallsToMethod { get; set; }

      // this member should become public in the stub
      protected int CallsToMethodProtected => CallsToMethod;
      
      // it should be possible to override this method
      // it should be possible to call the base class implementation of this method via 'BaseMethodToCall1'
      // if not overriden, it should do the same thing as a call to 'BaseMethodToCall1'
      protected virtual void MethodToCall1() => CallsToMethod++;

      public void ForwardToMethodToCall1() => MethodToCall1(); // (included so that we can test that calls to MethodToCall1 work as desired, even though the method is protected.

      // it should be possible to call this method via 'MethodToCall2' (which is made public in the stub)
      // it should NOT be possible to override this method, since it is not virtual.
      protected void MethodToCall2() => CallsToMethod++;

      public void ForwardToMethodToCall2() => MethodToCall2(); // (included so that we can test that calls to MethodToCall2 work as desired, even though the method is protected.

      // it should be possible, but not required, to override this method in the stub
      public abstract void MethodToCall3();

      // it should be possible, but not required, to override this method in the stub
      protected abstract void MethodToCall4();

      public void ForwardToMethodToCall4() => MethodToCall4(); // (included so that we can test that calls to MethodToCall4 work as desired, even though the method is protected.

      // stub should have a default constructor that calls this
      public ExampleClass() { }

      // stub should have a constructor that calls this
      public ExampleClass(int number) => CallsToMethod = number;

      // stub should have a constructor that calls this
      // stub should have a DeferConstruction static method that correctly identifies this, even if a derived object type is passed in
      public ExampleClass(object someObject) => CallsToMethod = someObject?.ToString()?.Length ?? 0;
   }
}

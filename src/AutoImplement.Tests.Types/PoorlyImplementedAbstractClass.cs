using System.Linq;

namespace HavenSoft.AutoImplement.Tests.Types {
   // It's possbile for an abstract class to call an abstract method during the constructor.
   // This is bad practice, because the method comes from an object that hasn't had its constructor run yet.
   // This is because the base-class constructor completes before any subclass constructors are run.
   // For this reason, classes should not call any virtual or abstract members in their constructors.
   // A better solution would be to decompose the object into a main class and a strategy class.
   // Construct the strategy and pass it in. That way, you're only calling methods on fully constructed objects.

   // For Stubs, a class's constructor calling a virtual member means that it's difficult to assign an implementation for that member.
   // So a good Stub must include some way of assigning an implementation to a stub BEFORE that Stub runs any constructors.
   public abstract class PoorlyImplementedAbstractClass
   {
      public abstract string ConfigureSomething();
      public string ReversedConfiguration { get; }

      public PoorlyImplementedAbstractClass(int number) {
         var configuration = ConfigureSomething();      // this line is bad!
         var reverse = configuration.Reverse().ToArray();
         ReversedConfiguration = new string(reverse);
      }
   }
}

using System.Reflection;

[assembly: AssemblyTitle("System.Delegation")]
[assembly: AssemblyProduct("System.Delegation")]

namespace System.Delegation
{
   /// <example>
   /// this.MyProperty = new PropertyImplementation&lt;int&gt;();
   /// 
   /// this.MyProperty.set = value => Console.WriteLine(value);
   /// 
   /// this.MyProperty.get = () => 7;
   /// 
   /// this.MyProperty.value = 4;
   /// 
   /// this.MyProperty = 2;
   /// 
   /// int number = this.MyProperty;
   /// </example>
   /// <remarks>
   /// Implicitly casting a T to a new PropertyImplementation resets its delegates to the defaults.
   /// </remarks>
   public class PropertyImplementation<T>
   {
      public Func<T> get;

      public Action<T> set;

      public T value;

      public PropertyImplementation(T initialValue = default(T))
      {
         value = initialValue;
         set = input => value = input;
         get = () => value;
      }

      public static implicit operator T(PropertyImplementation<T> cast) => cast.get();

      public static implicit operator PropertyImplementation<T>(T cast) => new PropertyImplementation<T>(cast);
   }
}

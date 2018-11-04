using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   public partial class ClassCompileTests {
      internal static void AssertCompile(Type type) {
         Program.GenerateImplementations(InterfaceCompileTests.ThisAssembly, type.Name);

         var writer = new CSharpSourceWriter();
         var fileName = new StubBuilder(writer).GetDesiredOutputFileName(type);
         AssertCompile(fileName);
      }

      private static void AssertCompile(params string[] contents) {
         var provider = new CSharpCodeProvider();
         var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).Select(asm => asm.Location);

         var parameters = new CompilerParameters {
            ReferencedAssemblies = {
               assemblies.Single(asm => asm.Contains("AutoImplement.Tests")),
               assemblies.Single(asm => asm.Contains("System.Core")),
               assemblies.Single(asm => asm.Contains("System.dll")),
               new FileInfo("System.Delegation.dll").FullName,
            },
         };

         var results = provider.CompileAssemblyFromFile(parameters, contents);
         Assert.Empty(results.Errors);
         foreach (var file in contents) File.Delete(file);
      }
   }

   public class SimpleClass { }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildSimpleClass() => AssertCompile(typeof(SimpleClass));
   }

   public class ClassWithProperties {
      public int Property1 { get; set; }
      public int Property2 { get; }
      public int Property3 { set { } }
      public virtual int Property4 { get; }
      public virtual int Property5 { get; set; }
      public virtual int Property6 { set { } }

      public virtual int Property7 { get; protected set; }

      public virtual int Property8 { get; private set; }

      public virtual int Property9 { get; internal set; }

      public virtual int Property0 { get; internal protected set; }

      public virtual int this[int i] { set { } }
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildClassWithProperties() => AssertCompile(typeof(ClassWithProperties));
   }

   public abstract class AbstractClassWithProperties {
      public int Property1 { get; set; }
      public virtual int Property2 { get; set; }
      public abstract int Property3 { get; set; }

      protected abstract string this[int w] { get; }
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildAbstractClassWithProperties() => AssertCompile(typeof(AbstractClassWithProperties));
   }

   public abstract class AbstractClassThatAbstractlyImplementsAnEventInterface : INotifyPropertyChanged {
      public abstract event PropertyChangedEventHandler PropertyChanged;
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildAbstractClassWithAbstractEventsFromInterface() => AssertCompile(typeof(AbstractClassThatAbstractlyImplementsAnEventInterface));
   }

   public abstract class AbstractClassThatAbstractlyImplementsAMethodInterface : IDisposable {
      public abstract void Dispose();
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildAbstractClassWithAbstractMethodFromInterface() => AssertCompile(typeof(AbstractClassThatAbstractlyImplementsAMethodInterface));
   }

   public abstract class AbstractClassWithMultipleInterfaceImplementations : INotifyPropertyChanged, IDisposable {
      public virtual event PropertyChangedEventHandler PropertyChanged;
      public abstract void Dispose();
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildAbstractClassWithMultipleVirtualInterfaceImplementations() => AssertCompile(typeof(AbstractClassWithMultipleInterfaceImplementations));
   }

   public abstract class ClassWithVirtualAndAbstractMethods {
      public abstract int Method1();
      public abstract void Method2(int value);
      public abstract bool Method3(out int number);

      public virtual int Method4() { return 7; }
      public virtual void Method5(int value) { }
      public virtual bool Method6(out int number) { number = 6; return true; }
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildClassWithVirtualAndAbstractMethods() => AssertCompile(typeof(ClassWithVirtualAndAbstractMethods));
   }

   public class ClassWithMultipleVirtualMethodsWithTheSameName {
      public virtual int Add(int a, int b) { return 0; }
      protected virtual double Add(double a, double b) => 0;
      public virtual void Add(string a, string b) { }
   }

   partial class ClassCompileTests {
      [Fact]
      public void CanBuildClassWithMultipleVirtualMethodsWithTheSameName() => AssertCompile(typeof(ClassWithMultipleVirtualMethodsWithTheSameName));
   }
}

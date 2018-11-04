using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Xunit;

namespace HavenSoft.AutoImplement.Tests {
   public partial class ClassCompileTests {
      internal static void AssertCompile(Type interfaceType) {
         Program.GenerateImplementations(InterfaceCompileTests.ThisAssembly, interfaceType.Name);

         var writer = new CSharpSourceWriter();
         AssertCompile(new StubBuilder(writer).GetDesiredOutputFileName(interfaceType));
      }

      private static void AssertCompile(params string[] contents) {
         var provider = new CSharpCodeProvider();
         var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).Select(asm => asm.Location);

         var parameters = new CompilerParameters {
            ReferencedAssemblies = {
               assemblies.Single(asm => asm.Contains("AutoImplement.Tests")),
               assemblies.Single(asm => asm.Contains("System.Core")),
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

   // TODO test with virtual/abstract methods
   // TODO test with virtual/abstract events
   // TODO test with multiple methods with the same name
}

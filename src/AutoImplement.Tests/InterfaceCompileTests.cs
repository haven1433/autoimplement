using HavenSoft.AutoImplement;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("AutoImplement.Tests")]
[assembly: AssemblyProduct("AutoImplement")]

namespace HavenSoft.AutoImplement.Tests {
   public class InterfaceCompileTests {
      public const string ThisAssembly = "AutoImplement.Tests.dll";

      [Fact]
      public void CanBuildFromGAC() {
         var mscorlib = typeof(int).Assembly.FullName;
         Program.GenerateImplementations(mscorlib, "IList`1");
         AssertCompile("StubList`1.cs", "CompositeList`1.cs", "ListDecorator`1.cs");
      }

      [Fact]
      public void CanBuildFromLocal() => AssertCompile(typeof(IEmptyInterface));

      [Fact]
      public void CanBuildCustomEvent() => AssertCompile(typeof(IHasCustomEvent));

      [Fact]
      public void CanBuildNormalEvent() => AssertCompile(typeof(IHasNormalEvent));

      [Fact]
      public void CanBuildStandardEvent() => AssertCompile(typeof(IUseEvent));

      [Fact]
      public void CanMakeGetProperty() => AssertCompile(typeof(IHaveGetProperty));

      [Fact]
      public void CanMakeSetProperty() => AssertCompile(typeof(IHaveSetProperty));

      [Fact]
      public void CanMakeGenericProperty() => AssertCompile(typeof(IHaveGenericProperty<>));

      [Fact]
      public void CanMakeTypeWithConflictingMethodNames() => AssertCompile(typeof(IHaveConflictingMethods));

      [Fact]
      public void CanMakeTypesWithOutParameterMethods() => AssertCompile(typeof(IHaveOutMethods));

      [Fact]
      public void CanMakeTypeWithGenericMethods() => AssertCompile(typeof(IHaveGenericMethods));

      [Fact]
      public void CanGenerateFromInGenericType() => AssertCompile(typeof(IInputInterface<>));

      [Fact]
      public void CanGenerateFromOutGenericType() => AssertCompile(typeof(IOutputInterface<>));

      [Fact]
      public void CanGenerateFromMultipleGenericConstraints() => AssertCompile(typeof(IInterfaceWithMultipleConstraints<>));

      [Fact]
      public void CanGenerateFromMethodConstraints() => AssertCompile(typeof(IInterfaceWithTypeConstrainedMethods));

      [Fact]
      public void CanGenerateFromInterfaceWithBaseInterfaceEvents() => AssertCompile(typeof(IInterfaceWithBaseEventInterface));

      private static void AssertCompile(Type interfaceType) {
         Program.GenerateImplementations(ThisAssembly, interfaceType.Name);

         var writer = new CSharpSourceWriter();
         AssertCompile(
            new StubBuilder(writer).GetDesiredOutputFileName(interfaceType),
            new CompositeBuilder(writer).GetDesiredOutputFileName(interfaceType),
            new DecoratorBuilder(writer).GetDesiredOutputFileName(interfaceType));
      }

      private static void AssertCompile(params string[] contents) {
         var provider = new CSharpCodeProvider();
         var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).Select(asm => asm.Location);

         var parameters = new CompilerParameters {
            ReferencedAssemblies = {
               assemblies.Single(asm => asm.Contains("AutoImplement.Tests.dll")),
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
}

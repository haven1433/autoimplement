using AutoImplement;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("AutoImplement")]
[assembly: AssemblyProduct("AutoImplement")]

namespace AutoImplementTests
{
   public class CompileTests {
      const string ThisAssembly = "AutoImplementTests.dll";

      [Fact]
      public void CanBuildFromGAC() {
         var mscorlib = typeof(int).Assembly.FullName;
         Program.GenerateImplementations(mscorlib, "IList`1");
         AssertCompile("StubList`1.cs", "CompositeList`1.cs", "ListDecorator`1.cs");
      }

      [Fact]
      public void CanBuildFromLocal() {
         Program.GenerateImplementations(ThisAssembly, nameof(IEmptyInterface));
         AssertCompile("StubEmptyInterface.cs", "CompositeEmptyInterface.cs", "EmptyInterfaceDecorator.cs");
      }

      [Fact]
      public void CanBuildCustomEvent() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHasCustomEvent));
         AssertCompile("StubHasCustomEvent.cs", "CompositeHasCustomEvent.cs", "HasCustomEventDecorator.cs");
      }

      [Fact]
      public void CanBuildNormalEvent() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHasNormalEvent));
         AssertCompile("StubHasNormalEvent.cs", "CompositeHasNormalEvent.cs", "HasNormalEventDecorator.cs");
      }

      [Fact]
      public void CanBuildStandardEvent() {
         Program.GenerateImplementations(ThisAssembly, nameof(IUseEvent));
         AssertCompile("StubUseEvent.cs", "CompositeUseEvent.cs", "UseEventDecorator.cs");
      }

      [Fact]
      public void CanMakeGetProperty() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHaveGetProperty));
         AssertCompile("StubHaveGetProperty.cs", "CompositeHaveGetProperty.cs", "HaveGetPropertyDecorator.cs");
      }

      [Fact]
      public void CanMakeSetProperty() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHaveSetProperty));
         AssertCompile("StubHaveSetProperty.cs", "CompositeHaveSetProperty.cs", "HaveSetPropertyDecorator.cs");
      }

      [Fact]
      public void CanMakeGenericProperty() {
         Program.GenerateImplementations(ThisAssembly, "IHaveGenericProperty`1");
         AssertCompile("StubHaveGenericProperty`1.cs", "CompositeHaveGenericProperty`1.cs", "HaveGenericPropertyDecorator`1.cs");
      }

      [Fact]
      public void CanMakeTypeWithConflictingMethodNames() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHaveConflictingMethods));
         AssertCompile("StubHaveConflictingMethods.cs", "CompositeHaveConflictingMethods.cs", "HaveConflictingMethodsDecorator.cs");
      }

      [Fact]
      public void CanMakeTypesWithOutParameterMethods() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHaveOutMethods));
         AssertCompile("StubHaveOutMethods.cs", "CompositeHaveOutMethods.cs", "HaveOutMethodsDecorator.cs");
      }

      [Fact]
      public void CanMakeTypeWithGenericMethods() {
         Program.GenerateImplementations(ThisAssembly, nameof(IHaveGenericMethods));
         AssertCompile("StubHaveGenericMethods.cs", "CompositeHaveGenericMethods.cs", "HaveGenericMethodsDecorator.cs");
      }

      private static void AssertCompile(params string[] contents) {
         var provider = new CSharpCodeProvider();
         var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).Select(asm => asm.Location);

         var parameters = new CompilerParameters {
            ReferencedAssemblies = {
               assemblies.Single(asm => asm.Contains("AutoImplementTests")),
               assemblies.Single(asm => asm.Contains("System.Core")),
               new FileInfo("System.Delegation.dll").FullName,
            },
         };

         var results = provider.CompileAssemblyFromFile(parameters, contents);
         Assert.False(results.Errors.HasErrors);
         foreach (var file in contents) File.Delete(file);
      }
   }
}

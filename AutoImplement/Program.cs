using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: AssemblyTitle("AutoImplement")]
[assembly: AssemblyProduct("AutoImplement")]

namespace AutoImplement {
   public static class Program {
      public static void Main(params string[] args) {
         if (args.Length == 0) {
            Console.WriteLine("Expected usage: AutoImplement <assembly> <type> <type> <type> ...");
            Console.WriteLine("If no types are included, implementatiosn will be created for every public interface.");
            Console.WriteLine("Example usage:");
            Console.WriteLine("   AutoImplement \"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" IDisposable");
            Console.WriteLine("   AutoImplement mscorlib IList`1");
            Console.WriteLine("   AutoImplement path/to/my/assembly.dll IMyType");
            Console.WriteLine("   AutoImplement System ICommand");
            Console.WriteLine("");
            Console.WriteLine("Assembly name can either be fully qualified from the GAC or a file path.");
            Console.WriteLine("Some common assemblies are preloaded so you can refer to them by their short name.");
            Console.WriteLine("Namespaces on types are optional.");
            return;
         }

         GenerateImplementations(args[0], args.Skip(1).ToArray());
      }

      public static void GenerateImplementations(string assemblyName, params string[] typeNames) {
         if (!TryGetAssembly(assemblyName, out var assembly)) return;

         if (typeNames.Length == 0) {
            typeNames = assembly.ExportedTypes.Where(type => type.IsInterface).Select(type => type.FullName).ToArray();
         }

         foreach (var typeName in typeNames) {
            if (!TryFindType(assembly, typeName, out var type)) return;

            var genericInformation = type.Name.Contains("`") ? "`" + type.Name.Split('`')[1] : string.Empty;
            var mainName = type.Name.Split('`')[0].Substring(1);

            var path = $"Stub{mainName}{genericInformation}.cs";
            var contents = new StubBuilder().GenerateImplementation(type, string.Empty, "System.Delegation");
            File.WriteAllText(path, contents);

            path = $"Composite{mainName}{genericInformation}.cs";
            contents = new CompositeBuilder().GenerateImplementation(type, $"System.Collections.Generic.List<{type.CreateCsName(type.Namespace)}>,", "System.Linq");
            File.WriteAllText(path, contents);

            path = $"{mainName}Decorator{genericInformation}.cs";
            contents = new DecoratorBuilder().GenerateImplementation(type, string.Empty, string.Empty);
            File.WriteAllText(path, contents);
         }
      }

      /// <summary>
      /// The user could've passed us a string representing the long name of an assembly in the GAC.
      /// They also could've passed us a string representing a file path on disk.
      /// We don't know which it is, so just try both.
      /// </summary>
      private static bool TryGetAssembly(string candidate, out Assembly result) {
         return TryGetAssemblyFromLongName(candidate, out result) || 
                TryGetAssemblyFromFile(candidate, out result);
      }

      private static bool TryGetAssemblyFromLongName(string name, out Assembly result) {
         try {
            result = Assembly.Load(name);
            return true;
         } catch (Exception) {
            result = null;
            return false;
         }
      }

      private static bool TryGetAssemblyFromFile(string file, out Assembly result) {
         try {
            result = Assembly.LoadFrom(file);
            return true;
         } catch (Exception ex) {
            Console.WriteLine($"Unable to load assembly: {ex.Message}");
            result = null;
            return false;
         }
      }

      private static bool TryFindType(Assembly assembly, string typeName, out Type result) {
         var similarOptions = new List<string>();
         foreach (var type in assembly.ExportedTypes) {
            if (type.FullName.ToUpper().Contains(typeName.ToUpper())) similarOptions.Add(type.FullName);
            if (type.Name == typeName || type.FullName == typeName) {
               result = type;
               if (!type.IsInterface) Console.WriteLine($"{type.FullName} is not an interface type.");
               return type.IsInterface;
            }
         }

         Console.WriteLine($"Unable to find type {typeName} in {assembly.FullName}.");
         if (similarOptions.Count != 0) {
            Console.WriteLine("Found these similar names: ");
            similarOptions.ForEach(Console.WriteLine);
         }
         result = null;
         return false;
      }
   }
}

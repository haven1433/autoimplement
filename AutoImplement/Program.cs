using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;

[assembly: AssemblyTitle("AutoImplement")]
[assembly: AssemblyProduct("AutoImplement")]

namespace AutoImplement {
   public static class Program {
      public static void Main(params string[] args) {
         if (args.Length == 0) {
            PrintUsageInformation();
            PauseIfStartedByExplorer();
            return;
         }

         GenerateImplementations(args[0], args.Skip(1).ToArray());

         PauseIfStartedByExplorer();
      }

      public static void GenerateImplementations(string assemblyName, params string[] typeNames) {
         if (!TryGetAssembly(assemblyName, out var assembly)) {
            PrintUsageInformation();
            return;
         }

         if (typeNames.Length == 0) {
            typeNames = assembly.ExportedTypes.Where(type => type.IsInterface).Select(type => type.FullName).ToArray();
         }

         GenerateImplementations(assembly, typeNames);

         Console.WriteLine($"Done generating implementations from {typeNames.Length} interfaces.");
      }

      private static void GenerateImplementations(Assembly assembly, string[] typeNames) {
         foreach (var typeName in typeNames) {
            if (!TryFindType(assembly, typeName, out var type)) continue;

            var genericInformation = type.Name.Contains("`") ? "`" + type.Name.Split('`')[1] : string.Empty;
            var mainName = type.Name.Split('`')[0];
            if (mainName.StartsWith("I")) mainName = mainName.Substring(1); // strip leading 'I' from interface name

            GenerateImplementation<StubBuilder>(type, $"Stub{mainName}{genericInformation}.cs", "System,System.Collections.Generic,System.Delegation");
            GenerateImplementation<CompositeBuilder>(type, $"Composite{mainName}{genericInformation}.cs", "System.Linq");
            GenerateImplementation<DecoratorBuilder>(type, $"{mainName}Decorator{genericInformation}.cs");
         }
      }

      private static void PrintUsageInformation() {
         Console.WriteLine("Expected usage: AutoImplement <assembly> <type> <type> <type> ...");
         Console.WriteLine("If no types are included, implementatiosn will be created for every public interface.");
         Console.WriteLine("Example usage:");
         Console.WriteLine("   AutoImplement \"mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" IDisposable");
         Console.WriteLine("   AutoImplement path/to/my/assembly.dll IMyType");
         Console.WriteLine("   <drag an assembly onto the program>");
         Console.WriteLine("");
         Console.WriteLine("Assembly name can either be fully qualified from the GAC or a file path.");
         Console.WriteLine("Some common assemblies are preloaded so you can refer to them by their short name.");
         Console.WriteLine("Namespaces on types are optional.");
      }

      /// <see cref="https://stackoverflow.com/questions/2531837/how-can-i-get-the-pid-of-the-parent-process-of-my-application/2533287#2533287"/>
      private static void PauseIfStartedByExplorer() {
         var myId = Process.GetCurrentProcess().Id;
         var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {myId}";
         var search = new ManagementObjectSearcher("root\\CIMV2", query);
         var results = search.Get().GetEnumerator();
         results.MoveNext();
         var queryObj = results.Current;
         var parentId = (uint)queryObj["ParentProcessId"];
         var parent = Process.GetProcessById((int)parentId);

         if (parent.ProcessName == "explorer") {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
         }
      }

      /// <summary>
      /// The user could've passed us a string representing the long name of an assembly in the GAC.
      /// They also could've passed us a string representing a file path on disk.
      /// We don't know which it is, so just try both.
      /// </summary>
      private static bool TryGetAssembly(string candidate, out Assembly result) {
         if (TryGetAssemblyFromLongName(candidate, out result)) {
            return true;
         }

         if (TryGetAssemblyFromFile(candidate, out result)) {
            return true;
         }

         return false;
      }

      private static bool TryGetAssemblyFromLongName(string name, out Assembly result) {
         try {
            result = Assembly.Load(name);
            return true;
         } catch (Exception) {
            // don't show error information, because if the longname fails then we can try again with the string as a local file.
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

      /// <summary>
      /// Creates a Builder of the given generic type to implement the given interface.
      /// Output is placed in the given fileName.
      /// </summary>
      private static void GenerateImplementation<TPatternBuilder>(Type interfaceType, string fileName, string additionalUsings = null)
      where TPatternBuilder : IPatternBuilder {
         var writer = new StringWriter();
         var builder = (TPatternBuilder)Activator.CreateInstance(typeof(TPatternBuilder), writer);
         Console.WriteLine($"Generating {fileName} ...");
         writer.Write($"// this file was created by AutoImplement");
         if (!string.IsNullOrEmpty(additionalUsings)) {
            foreach (var additionalUsing in additionalUsings.Split(',')) {
               writer.Write($"using {additionalUsing};");
            }
         }
         writer.Write(string.Empty);

         writer.Write($"namespace {interfaceType.Namespace}");
         using (writer.Indent()) {
            writer.Write($"public class {builder.ClassDeclaration(interfaceType)}");
            using (writer.Indent()) {
               builder.AppendExtraMembers(interfaceType);
               foreach (var member in FindAllMembers(interfaceType)) {
                  var metadata = new MemberMetadata(member);
                  switch (member.MemberType) {
                     case MemberTypes.Method:   ImplementMethod  (member, metadata, builder); break;
                     case MemberTypes.Event:    ImplementEvent   (member, metadata, builder); break;
                     case MemberTypes.Property: ImplementProperty(member, metadata, builder); break;
                     default:
                        throw new NotImplementedException($"AutoImplement has no way to implement a {member.MemberType} member.");
                  }
               }
            }
         }

         File.WriteAllText(fileName, writer.ToString());
      }

      private static IList<MemberInfo> FindAllMembers(Type baseType) {
         var list = new List<MemberInfo>(baseType.GetMembers());
         list.AddRange(baseType.GetInterfaces().SelectMany(FindAllMembers).Distinct());
         return list;
      }

      private static void ImplementMethod(MemberInfo info, MemberMetadata metadata, IPatternBuilder builder) {
         var methodInfo = (MethodInfo)info;
         if (methodInfo.IsSpecialName) return;
         builder.AppendMethod(methodInfo, metadata);
      }

      private static void ImplementProperty(MemberInfo info, MemberMetadata metadata, IPatternBuilder builder) {
         var propertyInfo = (PropertyInfo)info;
         if (propertyInfo.Name == "Item" && propertyInfo.GetIndexParameters().Length != 0) {
            builder.AppendItemProperty(propertyInfo, metadata);
         } else {
            builder.AppendProperty(propertyInfo, metadata);
         }
      }

      private static void ImplementEvent(MemberInfo info, MemberMetadata metadata, IPatternBuilder builder) {
         var eventInfo = (EventInfo)info;
         builder.AppendEvent(eventInfo, metadata);
      }
   }
}

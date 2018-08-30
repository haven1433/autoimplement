using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   public class StubBuilder : Builder {
      private readonly List<string> implementedMethods = new List<string>();

      protected override string ClassNamePrefix => "Stub";

      protected override void AppendMethod(MethodInfo info, MemberMetadata method) {
         var delegateName = GetStubName(method.ReturnType, method.ParameterTypes);
         var typesExtension = SanitizeMethodName(method.ParameterTypes);

         var methodsWithMatchingNameButNotSignature = implementedMethods.Where(name => name.Split('(')[0] == method.Name && name != $"{method.Name}({method.ParameterTypes})");
         string localImplementationName = methodsWithMatchingNameButNotSignature.Any() ? $"{method.Name}_{typesExtension}" : method.Name;

         if (info.GetParameters().Any(p => p.ParameterType.IsByRef)) {
            localImplementationName = $"{method.Name}_{typesExtension}";
            delegateName = $"{method.Name}Delegate_{typesExtension}";
            AppendLine($"public delegate {method.ReturnType} {delegateName}({method.ParameterTypesAndNames});" + Environment.NewLine);
         }

         // only add a delegation property for the first method with a given signature
         // this is important for IEnumerable<T>.GetEnumerator() and IEnumerable.GetEnumerator() -> same name, same signature
         if (!implementedMethods.Any(name => name == $"{method.Name}({method.ParameterTypes})")) {
            AppendLine($"public {delegateName} {localImplementationName} {{ get; set; }}" + Environment.NewLine);
         }

         ImplementInterfaceMethod(info, localImplementationName, method);
         implementedMethods.Add($"{method.Name}({method.ParameterTypes})");
      }

      protected override void AppendProperty(PropertyInfo info, MemberMetadata property) {
         // define the backing field
         AppendLine($"public PropertyImplementation<{property.ReturnType}> {property.Name} = new PropertyImplementation<{property.ReturnType}>();" + Environment.NewLine);

         // define the interface's property
         AppendLine($"{property.ReturnType} {property.DeclaringType}.{property.Name}");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) AppendLine($"return this.{property.Name}.get();");
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) AppendLine($"this.{property.Name}.set(value);");
            }
         }
      }

      /// <summary>
      /// the 'Item' property in C# is special: it's exposed as this[]
      /// </summary>
      protected override void AppendItemProperty(PropertyInfo info, MemberMetadata property) {
         if (info.CanRead) {
            AppendLine($"public System.Func<{property.ParameterTypes}, {property.ReturnType}> get_Item = ({property.ParameterNames}) => default({property.ReturnType});" + Environment.NewLine);
         }

         if (info.CanWrite) {
            AppendLine($"public System.Action<{property.ParameterTypes}, {property.ReturnType}> set_Item = ({property.ParameterNames}, value) => {{}};" + Environment.NewLine);
         }

         AppendLine($"{property.ReturnType} {property.DeclaringType}.this[{property.ParameterTypesAndNames}]");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) AppendLine($"return get_Item({property.ParameterNames});");
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) AppendLine($"set_Item({property.ParameterNames}, value);");
            }
         }
      }

      protected override void AppendEvent(EventInfo info, MemberMetadata eventData) {
         AppendLine($"public EventImplementation<{eventData.HandlerArgsType}> {info.Name} = new EventImplementation<{eventData.HandlerArgsType}>();");
         AppendLine(string.Empty);
         AppendLine($"event {eventData.HandlerType} {eventData.DeclaringType}.{info.Name}");
         using (Indent()) {
            AppendLine("add");
            using (Indent()) {
               AppendLine($"{info.Name}.add(new System.EventHandler<{eventData.HandlerArgsType}>(value));");
            }
            AppendLine("remove");
            using (Indent()) {
               AppendLine($"{info.Name}.remove(new System.EventHandler<{eventData.HandlerArgsType}>(value));");
            }
         }
      }

      private static string GetStubName(string returnType, string parameterTypes) {
         if (returnType == "void") {
            var delegateName = "System.Action";
            if (parameterTypes != string.Empty) delegateName += $"<{parameterTypes}>";
            return delegateName;
         } else {
            var delegateName = "System.Func";
            delegateName += parameterTypes == string.Empty ? $"<{returnType}>" : $"<{parameterTypes}, {returnType}>";
            return delegateName;
         }
      }

      private static string GetDefaultClause(string returnType) {
         return returnType == "void" ? string.Empty : $"default({returnType});";
      }
      
      /// <summary>
      /// When converting type lists into extensions to put on the end of method names,
      /// we have to sanitize them by removing characters that are illegal in C# member names.
      /// </summary>
      private static string SanitizeMethodName(string name) {
         return name
            .Replace(", ", "_")
            .Replace(">", "_")
            .Replace("<", "_")
            .Replace(".", "_");
      }

      private void ImplementInterfaceMethod(MethodInfo info, string localImplementationName, MemberMetadata method) {
         var call = $"this.{localImplementationName}";

         AppendLine($"{method.ReturnType} {method.DeclaringType}.{method.Name}({method.ParameterTypesAndNames})");
         using (Indent()) {
            AssignDefaultValuesToOutParameters(info);

            AppendLine($"if ({call} != null)");
            using (Indent()) {
               var returnClause = method.ReturnType == "void" ? string.Empty : "return ";
               AppendLine($"{returnClause}{call}({method.ParameterNames});");
            }
            if (method.ReturnType != "void") {
               AppendLine("else");
               using (Indent()) {
                  AppendLine($"return default({method.ReturnType});");
               }
            }
         }
      }
   }
}

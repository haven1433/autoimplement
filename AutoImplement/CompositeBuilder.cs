using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   public class CompositeBuilder : Builder {
      private readonly List<string> implementedMethods = new List<string>();

      protected override string ClassNamePrefix => "Composite";

      protected override void AppendMethod(MethodInfo info, MemberMetadata method) {
         var parent = info.DeclaringType.CreateCsName(info.DeclaringType.Namespace);

         // Use an explicit implementation only if the signature has already been used
         // example: IEnumerable<T>, which extends IEnumerable
         if (!implementedMethods.Any(name => name == $"{method.Name}({method.ParameterTypes})")) {
            AppendLine($"public {method.ReturnType} {method.Name}({method.ParameterTypesAndNames})");
         } else {
            AppendLine($"{method.ReturnType} {parent}.{method.Name}({method.ParameterTypesAndNames})");
         }

         using (Indent()) {
            AssignDefaultValuesToOutParameters(info);

            if (method.ReturnType == "void") {
               AppendLine($"for (int i = 0; i < base.Count; i++)");
               using (Indent())
               {
                  AppendLine($"base[i].{method.Name}({method.ParameterNames});");
               }
            } else {
               AppendLine($"var results = new System.Collections.Generic.List<{method.ReturnType}>();");
               AppendLine($"for (int i = 0; i < base.Count; i++)");
               using (Indent())
               {
                  AppendLine($"results.Add(base[i].{method.Name}({method.ParameterNames}));");
               }
               AppendLine($"return results.Count > 0 && results.All(result => result.Equals(results[0])) ? results[0] : default({method.ReturnType});");
            }
         }

         implementedMethods.Add($"{method.Name}({method.ParameterTypes})");
      }

      protected override void AppendProperty(PropertyInfo info, MemberMetadata property) {
         // define the backing field
         AppendLine($"public {property.ReturnType} {property.Name}");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) {
                  AppendLine($"var results = this.Select<{property.DeclaringType}, {property.ReturnType}>(listItem => listItem.{property.Name}).ToList();");
                  AppendLine($"return results.Count > 0 && results.All(result => result.Equals(results[0])) ? results[0] : default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) {
                  AppendLine($"this.ForEach(listItem => listItem.{property.Name} = value);");
               }
            }
         }
      }

      /// <summary>
      /// the 'Item' property in C# is special: it's exposed as this[]
      /// </summary>
      protected override void AppendItemProperty(PropertyInfo info, MemberMetadata property) {
         AppendLine($"public {property.ReturnType} this[{property.ParameterTypesAndNames}]");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) {
                  AppendLine($"var results = this.Select<{property.DeclaringType}, {property.ReturnType}>(listItem => listItem[{property.ParameterNames}]).ToList();");
                  AppendLine($"return results.All(result => result.Equals(results[0])) ? results[0] : default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) AppendLine($"this.ForEach(listItem => listItem[{property.ParameterNames}] = value);");
            }
         }
      }

      protected override void AppendEvent(EventInfo info, MemberMetadata eventData) {
         AppendLine($"public event {eventData.HandlerType} {eventData.Name}");
         using (Indent()) {
            AppendLine("add");
            using (Indent()) {
               AppendLine($"this.ForEach(listItem => listItem.{eventData.Name} += value);");
            }
            AppendLine("remove");
            using (Indent()) {
               AppendLine($"this.ForEach(listItem => listItem.{eventData.Name} -= value);");
            }
         }
      }
   }
}

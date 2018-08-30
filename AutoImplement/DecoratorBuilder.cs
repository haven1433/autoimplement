using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   public class DecoratorBuilder : Builder {
      private readonly List<string> implementedMethods = new List<string>();

      private string innerObject;

      protected override string ClassNamePostfix => "Decorator";

      protected override void IncludeExtraMembers(Type type) {
         base.IncludeExtraMembers(type);
         innerObject = GetInnerObjectName(type);
         var typeName = type.CreateCsName(type.Namespace);
         AppendLine($"protected {typeName} {innerObject} {{ get; set; }}");
      }

      protected override void AppendMethod(MethodInfo info, MemberMetadata method) {
         var returnClause = method.ReturnType == "void" ? string.Empty : "return ";

         // Use an explicit implementation only if the signature has already been used
         // example: IEnumerable<T>, which extends IEnumerable
         // since expicit implementation can't be virtual, have the explicit one call the normal one.
         // In the case of Enumerable, this is correct. Hopefully there aren't too many interfaces that makes this happen, cuz it's WEIRD.
         // When it does happen, hopefully the creator of the child interface wanted the child method to behave as the parent method... in which case this is the correct implementation.
         if (implementedMethods.Any(name => name == $"{method.Name}({method.ParameterTypes})")) {
            AppendLine($"{method.ReturnType} {method.DeclaringType}.{method.Name}({method.ParameterTypesAndNames})");
            using (Indent()) {
               AppendLine($"{returnClause}{method.Name}({method.ParameterNames});");
            }
            return;
         }

         AppendLine($"public virtual {method.ReturnType} {method.Name}({method.ParameterTypesAndNames})");
         using (Indent()) {
            AssignDefaultValuesToOutParameters(info);
            AppendLine($"if ({innerObject} != null)");
            using (Indent()) {
               AppendLine($"{returnClause}{innerObject}.{method.Name}({method.ParameterNames});");
            }
            if (returnClause != string.Empty) {
               AppendLine("else");
               using (Indent()) {
                  AppendLine($"return default({method.ReturnType});");
               }
            }
         }

         implementedMethods.Add($"{method.Name}({method.ParameterTypes})");
      }

      protected override void AppendProperty(PropertyInfo info, MemberMetadata property) {
         AppendLine($"public virtual {property.ReturnType} {property.Name}");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) {
                  AppendLine($"if ({innerObject} != null)");
                  using (Indent()) AppendLine($"return {innerObject}.{info.Name};");
                  AppendLine($"else");
                  using (Indent()) AppendLine($"return default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) {
                  AppendLine($"if ({innerObject} != null)");
                  using (Indent()) AppendLine($"{innerObject}.{info.Name} = value;");
               }
            }
         }
      }

      protected override void AppendItemProperty(PropertyInfo info, MemberMetadata property) {
         AppendLine($"public virtual {property.ReturnType} this[{property.ParameterTypesAndNames}]");
         using (Indent()) {
            if (info.CanRead) {
               AppendLine("get");
               using (Indent()) {
                  AppendLine($"if ({innerObject} != null)");
                  using (Indent()) AppendLine($"return {innerObject}[{property.ParameterNames}];");
                  AppendLine("else");
                  using (Indent()) AppendLine($"return default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               AppendLine("set");
               using (Indent()) {
                  AppendLine($"if ({innerObject} != null)");
                  using (Indent()) AppendLine($"{innerObject}[{property.ParameterNames}] = value;");
               }
            }
         }
      }

      protected override void AppendEvent(EventInfo info, MemberMetadata eventData) {
         AppendLine($"public virtual event {eventData.HandlerType} {eventData.Name}");
         using (Indent()) {
            AppendLine("add");
            using (Indent()) {
               AppendLine($"if ({innerObject} != null)");
               using (Indent()) {
                  AppendLine($"{innerObject}.{eventData.Name} += value;");
               }
            }
            AppendLine("remove");
            using (Indent()) {
               AppendLine($"if ({innerObject} != null)");
               using (Indent()) {
                  AppendLine($"{innerObject}.{eventData.Name} -= value;");
               }
            }
         }
      }

      private string GetInnerObjectName(Type type) {
         var parent = type.CreateCsName(type.Namespace);
         return "Inner" + parent.Split('<')[0].Substring(1);
      }
   }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   public class CompositeBuilder : IPatternBuilder {
      private readonly List<string> implementedMethods = new List<string>();

      private readonly StringWriter writer;

      public CompositeBuilder(StringWriter writer) => this.writer = writer;

      public string ClassDeclaration(Type interfaceType) {
         var interfaceName = interfaceType.CreateCsName(interfaceType.Namespace);
         var (basename, genericInfo) = interfaceName.ExtractImplementingNameParts();

         return $"Composite{basename}{genericInfo} : System.Collections.Generic.List<{interfaceName}>, {interfaceName}";
      }

      public void AppendExtraMembers(Type interfaceType) { }

      // <example>
      // public void DoThing(int arg)
      // {
      //    for (int i = 0; i < base.Count; i++)
      //    {
      //       base[i].DoThing(arg);
      //    }
      // }
      // </example>
      /// <remarks>
      /// Composite methods with return types are a bit strange.
      /// If all the methods agree on what to return, then return that.
      /// If any are different, then just return default.
      /// In the case of nullables and bools, this default seems appropriate.
      /// But it can be strange for numeric types.
      /// 
      /// For methods that return void, a composite simply forwards the method call down to each thing that it contains.
      /// </remarks>
      public void AppendMethod(MethodInfo info, MemberMetadata method) {
         // Use an explicit implementation only if the signature has already been used
         // example: IEnumerable<T>, which extends IEnumerable
         if (!implementedMethods.Any(name => name == $"{method.Name}({method.ParameterTypes})")) {
            writer.Write($"public {method.ReturnType} {method.Name}({method.ParameterTypesAndNames})");
         } else {
            writer.Write($"{method.ReturnType} {method.DeclaringType}.{method.Name}({method.ParameterTypesAndNames})");
         }

         using (writer.Indent()) {
            writer.AssignDefaultValuesToOutParameters(info.DeclaringType.Namespace, info.GetParameters());

            if (method.ReturnType == "void") {
               writer.Write("for (int i = 0; i < base.Count; i++)");
               using (writer.Indent()) {
                  writer.Write($"base[i].{method.Name}({method.ParameterNames});");
               }
            } else {
               writer.Write($"var results = new System.Collections.Generic.List<{method.ReturnType}>();");
               writer.Write("for (int i = 0; i < base.Count; i++)");
               using (writer.Indent()) {
                  writer.Write($"results.Add(base[i].{method.Name}({method.ParameterNames}));");
               }
               writer.Write("if (results.Count > 0 && results.All(result => result.Equals(results[0])))");
               using (writer.Indent()) {
                  writer.Write("return results[0];");
               }
               writer.Write($"return default({method.ReturnType});");
            }
         }

         implementedMethods.Add($"{method.Name}({method.ParameterTypes})");
      }

      // <example>
      // public event EventHandler MyEvent
      // {
      //    add
      //    {
      //       this.ForEach(listItem => listItem.MyEvent += value;
      //    }
      //    remove
      //    {
      //       this.ForEach(listItem => listItem.MyEvent -= value;
      //    }
      // }
      // </example>
      /// <remarks>
      /// Event implementations are allowed to provide bodies for what to do in the add/remove cases.
      /// In this case, the most appropriate action is just to forward the notification down to the contained items.
      /// </remarks>
      public void AppendEvent(EventInfo info, MemberMetadata eventData) {
         writer.Write($"public event {eventData.HandlerType} {eventData.Name}");
         using (writer.Indent()) {
            writer.Write("add");
            using (writer.Indent()) {
               writer.Write($"this.ForEach(listItem => listItem.{eventData.Name} += value);");
            }
            writer.Write("remove");
            using (writer.Indent()) {
               writer.Write($"this.ForEach(listItem => listItem.{eventData.Name} -= value);");
            }
         }
      }

      // <example>
      // public string MyProperty
      // {
      //    get
      //    {
      //       var results = this.Select(listItem => listItem.MyProperty).ToList();
      //       return results.Count > 0 && results.All(result => result.Equals(results[0])) ? results[0] : default(string);
      //    }
      //    set
      //    { 
      //       this.ForEach(listItem => listItem.MyProperty = value);
      //    }
      // }
      // </example>
      /// <remarks>
      /// Get accessors work mostly the same way as methods with return types: return a single value, if they all match.
      /// This can be useful for aggregating similar results up, if the property is nullable.
      /// Set accessors work fine for Composites: just set the property for each item in the composite.
      /// </remarks>
      public void AppendProperty(PropertyInfo info, MemberMetadata property) {
         writer.Write($"public {property.ReturnType} {property.Name}");
         AppendPropertyCommon(info, property, $"listItem.{ property.Name}");
      }

      /// <remarks>
      /// Item properties are a bit special in .Net, since they're exposed as the [] accessor.
      /// However, for composites, they're much the same as normal propreties.
      /// </remarks>
      public void AppendItemProperty(PropertyInfo info, MemberMetadata property) {
         writer.Write($"public {property.ReturnType} this[{property.ParameterTypesAndNames}]");
         AppendPropertyCommon(info, property, $"listItem[{property.ParameterNames}]");
      }

      private void AppendPropertyCommon(PropertyInfo info, MemberMetadata property, string listItem) {
         using (writer.Indent()) {
            if (info.CanRead) {
               writer.Write("get");
               using (writer.Indent()) {
                  writer.Write($"var results = this.Select<{property.DeclaringType}, {property.ReturnType}>(listItem => {listItem}).ToList();");
                  writer.Write($"return results.Count > 0 && results.All(result => result.Equals(results[0])) ? results[0] : default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               writer.Write("set");
               using (writer.Indent()) {
                  writer.Write($"this.ForEach(listItem => {listItem} = value);");
               }
            }
         }
      }
   }
}

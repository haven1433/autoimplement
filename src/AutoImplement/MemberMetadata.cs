﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static System.Reflection.GenericParameterAttributes;

namespace HavenSoft.AutoImplement {
   public class MemberMetadata {
      public string DeclaringType { get; }
      public string Name { get; }
      public string ReturnType { get; }
      public string HandlerType { get; }
      public string HandlerArgsType { get; }
      public string ParameterTypes { get; }
      public string ParameterNames { get; }
      public string ParameterTypesAndNames { get; }
      public string GenericParameters { get; } = string.Empty;
      public string GenericParameterConstraints { get; } = string.Empty;

      public MemberMetadata(MemberInfo info) {
         var declaringNamespace = info.DeclaringType.Namespace;
         DeclaringType = info.DeclaringType.CreateCsName(declaringNamespace);
         Name = info.Name;

         if (info is MethodInfo methodInfo) {
            ReturnType = methodInfo.ReturnType.CreateCsName(declaringNamespace);
            (ParameterTypes, ParameterNames, ParameterTypesAndNames) = BuildArgumentLists(methodInfo.GetParameters());
            if (methodInfo.IsGenericMethodDefinition) {
               GenericParameters = GetGenericParamterList(methodInfo);
               GenericParameterConstraints = GetGenericParameterConstraints(methodInfo.GetGenericArguments(), declaringNamespace);
            }
         } else if (info is PropertyInfo propertyInfo) {
            ReturnType = propertyInfo.PropertyType.CreateCsName(declaringNamespace);
            var paramList = propertyInfo.GetIndexParameters();
            (ParameterTypes, ParameterNames, ParameterTypesAndNames) = BuildArgumentLists(paramList);
         } else if (info is EventInfo eventInfo) {
            HandlerType = eventInfo.EventHandlerType.CreateCsName(declaringNamespace);
            var eventSignature = eventInfo.EventHandlerType.GetMethod("Invoke");
            var eventArgsType = eventSignature.GetParameters()[1].ParameterType;
            HandlerArgsType = eventArgsType.CreateCsName(declaringNamespace);
         } else if (info is ConstructorInfo constructorInfo) {
            (ParameterTypes, ParameterNames, ParameterTypesAndNames) = BuildArgumentLists(constructorInfo.GetParameters());
         }
      }

      /// <example>
      /// where T : SomeClass, ISomeInterface, new()
      /// </example>
      public static string GetGenericParameterConstraints(Type[] genericArgs, string containingNamespace) {
         var result = string.Empty;

         foreach (var arg in genericArgs) {
            var constraints = new List<string>();
            foreach (var constraint in arg.GetGenericParameterConstraints()) {
               var typeConstraint = constraint.CreateCsName(containingNamespace);
               if (typeConstraint == "System.ValueType" || (containingNamespace == "System" && typeConstraint == "ValueType")) {
                  // Cannot constrain by System.ValueType in C#. Constrain using 'struct' instead.
               } else {
                  constraints.Add(typeConstraint);
               }
            }

            var attributes = arg.GenericParameterAttributes;
            if (attributes.HasFlag(ReferenceTypeConstraint)) constraints.Add("class");
            if (attributes.HasFlag(NotNullableValueTypeConstraint)) constraints.Add("struct");
            if (attributes.HasFlag(DefaultConstructorConstraint)) {
               if (attributes.HasFlag(NotNullableValueTypeConstraint)) {
                  // all structs have a new() method by default: no need to add the constraint
               } else {
                  constraints.Add("new()");
               }
            }

            if (constraints.Count == 0) continue;
            result += $" where {arg.Name} : " + constraints.Aggregate((a, b) => $"{a}, {b}");
         }

         return result;
      }

      private static (string types, string names, string typesAndNames) BuildArgumentLists(ParameterInfo[] parameters) {
         var types = new List<string>();
         var names = new List<string>();
         var typesAndNames = new List<string>();
         string aggregate(List<string> list) => list.Count == 0 ? string.Empty : list.Aggregate((a, b) => $"{a}, {b}");

         foreach (var info in parameters) {
            var typeName = info.ParameterType.CreateCsName(info.Member.DeclaringType.Namespace);
            string modifier = info.ParameterType.IsByRef && !info.IsOut ? "ref " :
               info.ParameterType.IsByRef && info.IsOut ? "out " :
               string.Empty;
            types.Add(typeName);
            names.Add($"{modifier}{info.Name}");
            typesAndNames.Add($"{modifier}{typeName} {info.Name}");
         }

         return (aggregate(types), aggregate(names), aggregate(typesAndNames));
      }

      /// <example>
      /// &lt;T1, T2&gt;
      /// &lt;in T&gt;
      /// </example>
      private static string GetGenericParamterList(MethodInfo methodInfo) {
         var args = methodInfo.GetGenericArguments();

         var genericParameterNames = new List<string>();

         foreach (var arg in args) {
            var modifiers = arg.GenericParameterAttributes;

            if (modifiers.HasFlag(Covariant)) {
               genericParameterNames.Add($"in {arg.Name}");
            } else if (modifiers.HasFlag(Contravariant)) {
               genericParameterNames.Add($"out {arg.Name}");
            } else {
               genericParameterNames.Add(arg.Name);
            }
         }

         var list = genericParameterNames.Aggregate((a, b) => $"{a}, {b}");
         return $"<{list}>";
      }
   }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   public class MemberMetadata {
      public string DeclaringType { get; }
      public string Name { get; }
      public string ReturnType { get; }
      public string HandlerType { get; }
      public string HandlerArgsType { get; }
      public string ParameterTypes { get; }
      public string ParameterNames { get; }
      public string ParameterTypesAndNames { get; }

      public MemberMetadata(MemberInfo info) {
         DeclaringType = info.DeclaringType.CreateCsName(info.DeclaringType.Namespace);
         Name = info.Name;

         if (info is MethodInfo methodInfo) {
            ReturnType = methodInfo.ReturnType.CreateCsName(info.DeclaringType.Namespace);
            (ParameterTypes, ParameterNames, ParameterTypesAndNames) = BuildArgumentLists(methodInfo.GetParameters());
         } else if (info is PropertyInfo propertyInfo) {
            ReturnType = propertyInfo.PropertyType.CreateCsName(info.DeclaringType.Namespace);
            (ParameterTypes, ParameterNames, ParameterTypesAndNames) = BuildArgumentLists(propertyInfo.GetIndexParameters());
         } else if (info is EventInfo eventInfo) {
            HandlerType = eventInfo.EventHandlerType.CreateCsName(info.DeclaringType.Namespace);
            HandlerArgsType = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters()[1].ParameterType.CreateCsName(info.DeclaringType.Namespace);
         }
      }

      private static (string types, string names, string typesAndNames) BuildArgumentLists(ParameterInfo[] parameters)
      {
         var types = new List<string>();
         var names = new List<string>();
         var typesAndNames = new List<string>();
         string aggregate(List<string> list) => list.Count == 0 ? string.Empty : list.Aggregate((a, b) => $"{a}, {b}");

         foreach (var info in parameters)
         {
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
   }
}

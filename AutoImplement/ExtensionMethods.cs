using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoImplement
{
   public static class ExtensionMethods
   {
      // from https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table
      private static readonly IReadOnlyDictionary<string, string> typeList = new Dictionary<string, string>
      {
         { "System.Void",    "void" }, // void wasn't in the table, but C# doesn't allow explicit use of 'System.Void'
         { "System.Boolean", "bool" },
         { "System.Byte",    "byte" },
         { "System.SByte",   "sbyte" },
         { "System.Char",    "char" },
         { "System.Decimal", "decimal" },
         { "System.Double",  "double" },
         { "System.Float",   "float" },
         { "System.Int32",   "int" },
         { "System.UInt32",  "uint" },
         { "System.Int64",   "long" },
         { "System.UInt64",  "ulong" },
         { "System.Object",  "object" },
         { "System.Int16",   "short" },
         { "System.UInt16",  "ushort" },
         { "System.String",  "string" },
      };

      public static string CreateCsName(this Type type, string currentNamespace)
      {
         // dealing with generic arguments
         var genericArgList = type.GetGenericArguments().Select(arg => arg.CreateCsName(currentNamespace)).ToList();
         string genericArguments = genericArgList.Count == 0 ? string.Empty : "<" + genericArgList.Aggregate((a, b) => $"{a}, {b}") + ">";

         var result = type.FullName ?? type.Name;                                // example: 'T' (type is a generic argument)
         result = result.Replace("&", string.Empty);                             // remove the '&' appended to ref/out parameters
         result = result.Split('`')[0] + genericArguments;                       // example: List`1 -> List<T>
         if (typeList.ContainsKey(result)) result = typeList[result];            // example: System.Int32 -> int
         var scope = currentNamespace + ".";
         if (result.StartsWith(scope)) result = result.Substring(scope.Length);  // example: My.Current.Namespace.CustomType -> CustomType
         return result;
      }
   }
}

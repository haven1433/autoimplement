using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoImplement {
   /// <summary>
   /// To build a custom decorator, extend the class that this builds.
   /// </summary>
   public class DecoratorBuilder : IPatternBuilder {
      private readonly List<string> implementedMethods = new List<string>();

      private string innerObject;

      private readonly StringWriter writer;

      public DecoratorBuilder(StringWriter writer) => this.writer = writer;

      public string ClassDeclaration(Type interfaceType) {
         var interfaceName = interfaceType.CreateCsName(interfaceType.Namespace);
         var (basename, genericInfo) = interfaceName.ExtractImplementingNameParts();

         return $"{basename}Decorator{genericInfo} : {interfaceName}";
      }

      /// <remarks>
      /// A decorator needs access to the inner implementation.
      /// For an interface like IDisposable, name this member InnerDisposable.
      /// 
      /// The base class does nothing to set this. The extending type decides
      /// if the inner object should be set during construction, through a property,
      /// or maybe even not at all.
      /// </remarks>
      public void AppendExtraMembers(Type type) {
         innerObject = GetInnerObjectName(type);
         var typeName = type.CreateCsName(type.Namespace);
         writer.Write($"protected {typeName} {innerObject} {{ get; set; }}");
      }

      /// <example>
      // public virtual void Dispose()
      // {
      //    if (InnerDisposable != null)
      //    {
      //       InnerDisposable.Dispose();
      //    }
      // }
      /// </example>
      /// <remarks>
      /// Default implementations for decorator members do as little as possible.
      /// They forward to the inner object (if there is one), and otherwise just return defaults.
      /// All these members are virtual, so a subclass can optionally extend them to quickly build custom decorators.
      /// The idea is that a decorator that derives from a generated baseclass should contain no glue code.
      /// </remarks>
      public void AppendMethod(MethodInfo info, MemberMetadata method) {
         var returnClause = method.ReturnType == "void" ? string.Empty : "return ";

         // Use an explicit implementation only if the signature has already been used
         // example: IEnumerable<T>, which extends IEnumerable
         // since expicit implementation can't be virtual, have the explicit one call the normal one.
         // In the case of Enumerable, this is correct.
         // Hopefully there aren't too many interfaces that do this, cuz it's WEIRD.
         // When it does happen, hopefully the creator of the child interface wants the child to behave as the parent...
         // in which case this is the correct implementation.
         if (implementedMethods.Any(name => name == $"{method.Name}({method.ParameterTypes})")) {
            writer.Write($"{method.ReturnType} {method.DeclaringType}.{method.Name}({method.ParameterTypesAndNames})");
            using (writer.Indent()) {
               writer.Write($"{returnClause}{method.Name}({method.ParameterNames});");
            }
            return;
         }

         writer.Write($"public virtual {method.ReturnType} {method.Name}({method.ParameterTypesAndNames})");
         using (writer.Indent()) {
            writer.AssignDefaultValuesToOutParameters(info.DeclaringType.Namespace, info.GetParameters());

            IfHasInnerObject($"{returnClause}{innerObject}.{method.Name}({method.ParameterNames});");

            if (returnClause != string.Empty) {
               writer.Write($"return default({method.ReturnType});");
            }
         }

         implementedMethods.Add($"{method.Name}({method.ParameterTypes})");
      }

      // <example>
      // public virtual event EventHandler<MyEventArgs> MyEvent
      // {
      //    add
      //    {
      //       if (InnerThing != null)
      //       {
      //          InnerThing.MyEvent += value;
      //       }
      //    }
      //    remove
      //    {
      //       if (InnerThing != null)
      //       {
      //          InnerThing.MyEvent -= value;
      //       }
      //    }
      // }
      // </example>
      public void AppendEvent(EventInfo info, MemberMetadata eventData) {
         writer.Write($"public virtual event {eventData.HandlerType} {eventData.Name}");
         using (writer.Indent()) {
            writer.Write("add");
            using (writer.Indent()) {
               IfHasInnerObject($"{innerObject}.{eventData.Name} += value;");
            }
            writer.Write("remove");
            using (writer.Indent()) {
               IfHasInnerObject($"{innerObject}.{eventData.Name} -= value;");
            }
         }
      }

      /// <example>
      /// public virtual double CustomValue
      /// {
      ///    get
      ///    {
      ///       if (InnerBlob != null)
      ///       {
      ///          return InnerBlob.CustomValue;
      ///       }
      ///       return default(double);
      ///    }
      ///    set
      ///    {
      ///       if (InnerBlob != null)
      ///       {
      ///          InnerBlob.CustomValue = value;
      ///       }
      ///    }
      /// }
      /// </example>
      public void AppendProperty(PropertyInfo info, MemberMetadata property) {
         writer.Write($"public virtual {property.ReturnType} {property.Name}");
         AppendPropertyCommon(info, property, $"{innerObject}.{info.Name}");
      }

      /// <remarks>
      /// For decorators, Item properties are much the same as normal propreties.
      /// </remarks>
      public void AppendItemProperty(PropertyInfo info, MemberMetadata property) {
         writer.Write($"public virtual {property.ReturnType} this[{property.ParameterTypesAndNames}]");
         AppendPropertyCommon(info, property, $"{innerObject}[{property.ParameterNames}]");
      }

      private void AppendPropertyCommon(PropertyInfo info, MemberMetadata property, string member) {
         using (writer.Indent()) {
            if (info.CanRead) {
               writer.Write("get");
               using (writer.Indent()) {
                  IfHasInnerObject($"return {member};");
                  writer.Write($"return default({property.ReturnType});");
               }
            }
            if (info.CanWrite) {
               writer.Write("set");
               using (writer.Indent()) {
                  IfHasInnerObject($"{member} = value;");
               }
            }
         }
      }

      private string GetInnerObjectName(Type type) {
         var (name, genericInfo) = type.CreateCsName(type.Namespace).ExtractImplementingNameParts();

         // most interfaces start with a leading 'I' that we want to strip off
         if (name.StartsWith("I")) name = name.Substring(1);
         return $"Inner{name}";
      }

      private void IfHasInnerObject(string content) {
         writer.Write($"if ({innerObject} != null)");
         using (writer.Indent()) {
            writer.Write(content);
         }
      }
   }
}
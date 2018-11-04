using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HavenSoft.AutoImplement {
   /// <summary>
   /// Writing a Stub for a Class (as opposed to an interface) requires actually writing two types.
   /// One type will hold the new members that use the same names as the virtual, abstract, and overrides.
   /// The new members will allow for the implementation to be changed out on the fly.
   /// The other type will contain overrides for the class members that are virtual, abstract, or override.
   /// The second type will call the members of the first.
   /// The separation is needed because C# won't allow two members in the same type to have the same name.
   /// </summary>
   public class ClassStubBuilder : IPatternBuilder {

      private readonly List<string> implementedMethods = new List<string>();
      private readonly CSharpSourceWriter writer, helperWriter;
      private readonly Stack<IDisposable> helperScopes = new Stack<IDisposable>();
      private string stubTypeName;

      public ClassStubBuilder(CSharpSourceWriter writer) {
         // the main writer will write the Stub class, which contains the new members.
         this.writer = writer;
         writer.WriteUsings(
            "System",                     // Action, Func, Type
            "System.Collections.Generic", // Dictionary
            "System.Delegation");         // PropertyImplementation, EventImplementation

         // the helperWriter will write the intermediate class, which contains the override members.
         helperWriter = new CSharpSourceWriter(writer.Indentation);
      }

      public string GetDesiredOutputFileName(Type type) {
         var (mainName, genericInformation) = type.Name.ExtractImplementationNameParts("`");
         return $"Stub{mainName}{genericInformation}.cs";
      }

      public string ClassDeclaration(Type type) {
         var typeName = type.CreateCsName(type.Namespace);
         var (basename, genericInfo) = typeName.ExtractImplementationNameParts("<");
         var constraints = MemberMetadata.GetGenericParameterConstraints(type.GetGenericArguments(), type.Namespace);

         helperWriter.Write($"public class IntermediateStub{basename}_DoNotUse{genericInfo} : {typeName}{constraints}");
         helperScopes.Push(helperWriter.Scope);
         stubTypeName = $"Stub{basename}{genericInfo}";

         return $"{stubTypeName} : IntermediateStub{basename}_DoNotUse{genericInfo}{constraints}";
      }

      public void AppendExtraMembers(Type type) {
         var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public));

         foreach (var constructor in constructors) {
            var metadata = new MemberMetadata(constructor);
            AppendConstructor(type, constructor, metadata);
         }

         implementedMethods.Clear();
      }

      public void AppendMethod(MethodInfo info, MemberMetadata metadata) {
         if(info.IsSpecialName)
         if (info.IsStatic || info.IsPrivate || info.IsAssembly || info.IsFamilyAndAssembly) return;
         if (!info.IsVirtual && !info.IsAbstract) return;

         var access = info.IsFamily ? "protected" : "public";
         var delegateName = GetDelegateName(metadata.ReturnType, metadata.ParameterTypes);
         var returnClause = metadata.ReturnType != "void" ? "return " : string.Empty;

         var typesExtension = StubBuilder.SanitizeMethodName(metadata.ParameterTypes);

         var methodsWithMatchingNameButNotSignature = implementedMethods.Where(name => name.Split('(')[0] == metadata.Name && name != $"{metadata.Name}({metadata.ParameterTypes})");
         string localImplementationName = methodsWithMatchingNameButNotSignature.Any() ? $"{metadata.Name}_{typesExtension}" : metadata.Name;

         if (info.GetParameters().Any(p => p.ParameterType.IsByRef)) {
            delegateName = $"{metadata.Name}Delegate_{typesExtension}";
            writer.Write($"public delegate {metadata.ReturnType} {delegateName}({metadata.ParameterTypesAndNames});" + Environment.NewLine);
         }

         writer.Write($"public new {delegateName} {localImplementationName};");

         WriteHelperBaseMethod(info, metadata);
         WriteHelperMethod(info, metadata, access, stubTypeName, localImplementationName);

         implementedMethods.Add($"{metadata.Name}({metadata.ParameterTypes})");
      }

      public void AppendEvent(EventInfo info, MemberMetadata metadata) {
         var methodInfo = info.AddMethod;
         if (methodInfo.IsStatic || methodInfo.IsPrivate || methodInfo.IsAssembly || methodInfo.IsFamilyAndAssembly) return;
         if (!methodInfo.IsVirtual && !methodInfo.IsAbstract) return;

         var access = methodInfo.IsFamily ? "protected" : "public";

         writer.Write($"public new EventImplementation<{metadata.HandlerArgsType}> {metadata.Name} = new EventImplementation<{metadata.HandlerArgsType}>();");

         if (methodInfo.IsVirtual && !methodInfo.IsAbstract) {
            helperWriter.Write($"public void Base{metadata.Name}Add({metadata.HandlerType} e) {{ base.{metadata.Name} += e; }}");
            helperWriter.Write($"public void Base{metadata.Name}Remove({metadata.HandlerType} e) {{ base.{metadata.Name} -= e; }}");
         }

         helperWriter.Write($"{access} override event {metadata.HandlerType} {metadata.Name}");
         using (helperWriter.Scope) {
            helperWriter.Write($"add {{ (({stubTypeName})this).{metadata.Name}.add(new EventHandler<{metadata.HandlerArgsType}>(value)); }}");
            helperWriter.Write($"remove {{ (({stubTypeName})this).{metadata.Name}.remove(new EventHandler<{metadata.HandlerArgsType}>(value)); }}");
         }
      }

      public void AppendProperty(PropertyInfo info, MemberMetadata metadata) {
         var methodInfo = info.GetMethod ?? info.SetMethod;
         if (methodInfo.IsStatic || methodInfo.IsPrivate || methodInfo.IsAssembly || methodInfo.IsFamilyAndAssembly) return;
         if (!methodInfo.IsVirtual && !methodInfo.IsAbstract) return;

         var access = methodInfo.IsFamily ? "protected" : "public";

         writer.Write($"public new PropertyImplementation<{metadata.ReturnType}> {metadata.Name} = new PropertyImplementation<{metadata.ReturnType}>();");

         if (methodInfo.IsVirtual && !methodInfo.IsAbstract) {
            helperWriter.Write($"public {metadata.ReturnType} Base{metadata.Name}");
            using (helperWriter.Scope) {
               if (info.CanRead) helperWriter.Write($"get {{ return base.{metadata.Name}; }}");
               if (CanWrite(info)) helperWriter.Write($"set {{ base.{metadata.Name} = value; }}");
            }
         }

         helperWriter.Write($"{access} override {metadata.ReturnType} {metadata.Name}");
         using (helperWriter.Scope) {
            if (info.CanRead) helperWriter.Write($"get {{ return (({stubTypeName})this).{metadata.Name}.get(); }}");
            if (CanWrite(info)) {
               var setAccess = DeduceSetAccess(info);
               helperWriter.Write($"{setAccess}set {{ (({stubTypeName})this).{metadata.Name}.set(value); }}");
            }
         }
      }

      public void AppendItemProperty(PropertyInfo info, MemberMetadata metadata) {
         var methodInfo = info.GetMethod ?? info.SetMethod;
         if (methodInfo.IsStatic || methodInfo.IsPrivate || methodInfo.IsAssembly || methodInfo.IsFamilyAndAssembly) return;
         if (!methodInfo.IsVirtual && !methodInfo.IsAbstract) return;

         var access = methodInfo.IsFamily ? "protected" : "public";

         if (info.CanRead) writer.Write($"public new Func<{metadata.ParameterTypes}, {metadata.ReturnType}> get_Item;");
         if (info.CanWrite) writer.Write($"public new Action<{metadata.ParameterTypes}, {metadata.ReturnType}> set_Item;");

         if (methodInfo.IsVirtual && !methodInfo.IsAbstract) {
            if (info.CanRead) {
               helperWriter.Write($"public {metadata.ReturnType} Base_get_Item({metadata.ParameterTypesAndNames})");
               using (helperWriter.Scope) {
                  helperWriter.Write($"return base[{metadata.ParameterNames}];");
               }
            }
            if (info.CanWrite) {
               helperWriter.Write($"public void Base_set_Item({metadata.ParameterTypesAndNames}, {metadata.ReturnType} value)");
               using (helperWriter.Scope) {
                  helperWriter.Write($"base[{metadata.ParameterNames}] = value;");
               }
            }
         }

         helperWriter.Write($"{access} override {metadata.ReturnType} this[{metadata.ParameterTypesAndNames}]");
         using (helperWriter.Scope) {
            if (info.CanRead) helperWriter.Write($"get {{ return (({stubTypeName})this).get_Item({metadata.ParameterNames}); }}");
            if (info.CanWrite) helperWriter.Write($"set {{ (({stubTypeName})this).set_Item({metadata.ParameterNames}, value); }}");
         }
      }

      public void BuildCompleted() {
         while (helperScopes.Count > 0) helperScopes.Pop().Dispose();

         writer.Write(helperWriter.ToString());
      }

      private void WriteHelperBaseMethod(MethodInfo info, MemberMetadata metadata) {
         var returnClause = metadata.ReturnType != "void" ? "return " : string.Empty;

         if (info.IsVirtual && !info.IsAbstract) {
            helperWriter.Write($"public {metadata.ReturnType} Base{metadata.Name}{metadata.GenericParameterConstraints}({metadata.ParameterTypesAndNames})");
            using (helperWriter.Scope) {
               helperWriter.Write($"{returnClause}base.{metadata.Name}({metadata.ParameterNames});");
            }
         }
      }

      private void WriteHelperMethod(MethodInfo info, MemberMetadata metadata, string access, string stubTypeName, string localImplementationName) {
         var returnClause = metadata.ReturnType != "void" ? "return " : string.Empty;

         helperWriter.Write($"{access} override {metadata.ReturnType} {metadata.Name}({metadata.ParameterTypesAndNames}){metadata.GenericParameterConstraints}");
         using (helperWriter.Scope) {
            var call = $"(({stubTypeName})this).{localImplementationName}";
            helperWriter.AssignDefaultValuesToOutParameters(info.DeclaringType.Namespace, info.GetParameters());
            helperWriter.Write($"if ({call} != null)");
            using (helperWriter.Scope) {
               helperWriter.Write($"{returnClause}{call}({metadata.ParameterNames});");
            }
            if (metadata.ReturnType != "void") {
               helperWriter.Write("else");
               using (helperWriter.Scope) {
                  helperWriter.Write($"return default({metadata.ReturnType});");
               }
            }
         }
      }

      private void AppendConstructor(Type type, ConstructorInfo info, MemberMetadata constructorMetadata) {
         if (info.IsPrivate || info.IsStatic || info.IsFamilyAndAssembly || info.IsAssembly) return;
         var typeName = type.CreateCsName(type.Namespace);
         var (basename, genericInfo) = typeName.ExtractImplementationNameParts("<");
         var intermediateName = $"IntermediateStub{basename}_DoNotUse";
         var stubName = $"Stub{basename}";

         writer.Write($"public {stubName}({constructorMetadata.ParameterTypesAndNames})");
         using (writer.Scope) {
            foreach (var member in Program.FindAllMembers(type)) {
               
               var metadata = new MemberMetadata(member);
               switch (member.MemberType) {
                  case MemberTypes.Method: AppendToConstructorFromMethod((MethodInfo)member, metadata); break;
                  case MemberTypes.Event: AppendToConstructorFromEvent((EventInfo)member, metadata); break;
                  case MemberTypes.Property: AppendToConstructorFromProperty((PropertyInfo)member, metadata); break;
                  default:
                     // the only other options are Field, Type, NestedType, and Constructor
                     // none of those can be virtual/abstract, so we don't need to put anything in the constructor for them.
                     break;
               }
            }
         }

         helperWriter.Write($"protected {intermediateName}({constructorMetadata.ParameterTypesAndNames}) : base({constructorMetadata.ParameterNames}) {{ }}");
      }

      private void AppendToConstructorFromMethod(MethodInfo info, MemberMetadata metadata) {
         if (info.IsSpecialName || info.IsStatic || info.IsPrivate || !info.IsVirtual || info.IsAbstract || info.IsAssembly || info.IsFamilyAndAssembly) return;
         if (info.IsVirtual && info.Name == "Finalize") return; // Finalize is special in C#. Use a destructor instead.

         var typesExtension = StubBuilder.SanitizeMethodName(metadata.ParameterTypes);
         var methodsWithMatchingNameButNotSignature = implementedMethods.Where(name => name.Split('(')[0] == metadata.Name && name != $"{metadata.Name}({metadata.ParameterTypes})");
         string localImplementationName = methodsWithMatchingNameButNotSignature.Any() ? $"{metadata.Name}_{typesExtension}" : metadata.Name;

         writer.Write($"{localImplementationName} = Base{metadata.Name};");

         implementedMethods.Add($"{metadata.Name}({metadata.ParameterTypes})");
      }

      private void AppendToConstructorFromEvent(EventInfo info, MemberMetadata metadata) {
         var addMethod = info.AddMethod;
         if (addMethod.IsStatic || addMethod.IsPrivate || !addMethod.IsVirtual || addMethod.IsAbstract || addMethod.IsAssembly || addMethod.IsFamilyAndAssembly) return;

         writer.Write($"{metadata.Name}.add = value => Base{metadata.Name}Add(new {metadata.HandlerType}(value));");
         writer.Write($"{metadata.Name}.remove = value => Base{metadata.Name}Remove(new {metadata.HandlerType}(value));");
      }

      private void AppendToConstructorFromProperty(PropertyInfo info, MemberMetadata metadata) {
         var method = info.GetMethod ?? info.SetMethod;
         if (method.IsStatic || method.IsPrivate || !method.IsVirtual || method.IsAbstract || method.IsAssembly || method.IsFamilyAndAssembly) return;

         if (info.Name == "Item" && info.GetIndexParameters().Length > 0) {
            if (info.CanRead) writer.Write($"get_Item = Base_get_Item;");
            if (CanWrite(info)) writer.Write($"set_Item = Base_set_Item;");
         } else {
            if (info.CanRead) {
               writer.Write($"{metadata.Name}.get = () => Base{metadata.Name};");
            }
            if (CanWrite(info)) {
               writer.Write($"{metadata.Name}.set = value => Base{metadata.Name} = value;");
            }
         }
      }

      private static string DeduceSetAccess(PropertyInfo info) {
         if (!info.CanRead || !info.CanWrite) return string.Empty;
         if (info.GetMethod.IsPublic && info.SetMethod.IsPublic) return string.Empty;
         if (info.GetMethod.IsFamily && info.SetMethod.IsFamily) return string.Empty;
         if (info.GetMethod.IsFamilyOrAssembly && info.SetMethod.IsFamilyOrAssembly) return string.Empty;

         if (info.SetMethod.IsPublic) return "public ";
         if (info.SetMethod.IsFamily) return "protected ";
         if (info.SetMethod.IsFamilyOrAssembly) return "protected ";

         throw new NotImplementedException();
      }

      /// <summary>
      /// We're interested in whether or not a subclass in another assembly should provide an overload.
      /// PropertyInfo.CanWrite only tells us if there's a setter, not if we can overwrite it.
      /// </summary>
      private static bool CanWrite(PropertyInfo info) {
         if (!info.CanWrite) return false;
         if (info.SetMethod.IsPrivate) return false;
         if (info.SetMethod.IsAssembly) return false;
         if (info.SetMethod.IsFamilyAndAssembly) return false;
         return true;
      }

      private string GetDelegateName(string returnType, string parameterTypes) {
         if (returnType == "void") {
            var delegateName = "Action";
            if (parameterTypes != string.Empty) delegateName += $"<{parameterTypes}>";
            return delegateName;
         } else {
            var delegateName = "Func";
            delegateName += parameterTypes == string.Empty ? $"<{returnType}>" : $"<{parameterTypes}, {returnType}>";
            return delegateName;
         }
      }
   }
}

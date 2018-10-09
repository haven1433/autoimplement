using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoImplement {
   public class CSharpSourceWriter {
      private readonly StringBuilder builder = new StringBuilder();

      private readonly string indentation;

      private int indentLevel;

      /// <param name="numberOfSpacesToIndent">
      /// The number of spaces used for indenting in the source file.
      /// If this number is not positive, a single tab is used for indentation.
      /// </param>
      public CSharpSourceWriter(string usings, int numberOfSpacesToIndent = 0) {
         if (numberOfSpacesToIndent > 0) {
            indentation = new string(' ', numberOfSpacesToIndent);
         } else {
            indentation = "\t";
         }

         Write($"// this file was created by AutoImplement");
         if (!string.IsNullOrEmpty(usings)) {
            foreach (var additionalUsing in usings.Split(',')) {
               Write($"using {additionalUsing};");
            }
         }
         Write(string.Empty);
      }

      public void Write(string content) {
         var splitTokens = new[] { Environment.NewLine };
         var noSpecialOptions = StringSplitOptions.None;
         var currentIndentation = string.Concat(Enumerable.Repeat(indentation, indentLevel));
         foreach (var line in content.TrimStart().Split(splitTokens, noSpecialOptions)) {
            builder.AppendLine(currentIndentation + line);
         }
      }

      public IDisposable Scope => new IndentationScope(this);

      public void AssignDefaultValuesToOutParameters(string owningNamespace, ParameterInfo[] parameters) {
         foreach (var p in parameters.Where(p => p.IsOut)) {
            Write($"{p.Name} = default({p.ParameterType.CreateCsName(owningNamespace)});");
         }
      }

      public override string ToString() => builder.ToString();

      private class IndentationScope : IDisposable {
         private const int OneLevelOfIndentation = 4;

         private readonly CSharpSourceWriter parent;

         public IndentationScope(CSharpSourceWriter writer) {
            parent = writer;
            parent.Write("{");
            parent.indentLevel += OneLevelOfIndentation;
         }

         public void Dispose() {
            parent.indentLevel -= OneLevelOfIndentation;
            parent.Write("}");
         }
      }
   }
}

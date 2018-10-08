using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoImplement {
   public class StringWriter {
      private readonly StringBuilder builder = new StringBuilder();

      private int indent;

      public void Write(string content) {
         var splitTokens = new[] { Environment.NewLine };
         var noSpecialOptions = StringSplitOptions.None;
         foreach (var line in content.TrimStart().Split(splitTokens, noSpecialOptions)) {
            builder.AppendLine(new string(' ', indent) + line);
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

         private readonly StringWriter parent;

         public IndentationScope(StringWriter writer) {
            parent = writer;
            parent.Write("{");
            parent.indent += OneLevelOfIndentation;
         }

         public void Dispose() {
            parent.indent -= OneLevelOfIndentation;
            parent.Write("}");
         }
      }
   }
}

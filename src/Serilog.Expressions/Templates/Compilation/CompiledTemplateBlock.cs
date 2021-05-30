using System;
using System.IO;
using Serilog.Expressions;

namespace Serilog.Templates.Compilation
{
    class CompiledTemplateBlock : CompiledTemplate
    {
        readonly CompiledTemplate[] _elements;

        public CompiledTemplateBlock(CompiledTemplate[] elements)
        {
            _elements = elements ?? throw new ArgumentNullException(nameof(elements));
        }
        
        public override void Evaluate(EvaluationContext ctx, TextWriter output)
        {
            foreach (var element in _elements)
                element.Evaluate(ctx, output);
        }
    }
}

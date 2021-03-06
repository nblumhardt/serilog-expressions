using System;
using System.Text.RegularExpressions;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Expressions.Ast;
using Serilog.Expressions.Compilation.Transformations;

namespace Serilog.Expressions.Compilation.Text
{
    class LikeSyntaxTransformer: IdentityTransformer
    {
        static readonly LikeSyntaxTransformer Instance = new LikeSyntaxTransformer();

        public static Expression Rewrite(Expression expression)
        {
            return Instance.Transform(expression);
        }

        protected override Expression Transform(CallExpression lx)
        {
            if (lx.Operands.Length != 2)
                return base.Transform(lx);

            if (Operators.SameOperator(lx.OperatorName, Operators.IntermediateOpLike))
                return TryCompileLikeExpression(lx.IgnoreCase, lx.Operands[0], lx.Operands[1]);
            
            if (Operators.SameOperator(lx.OperatorName, Operators.IntermediateOpNotLike))
                return new CallExpression(
                    false,
                    Operators.RuntimeOpStrictNot,
                    TryCompileLikeExpression(lx.IgnoreCase, lx.Operands[0], lx.Operands[1]));

            return base.Transform(lx);
        }

        Expression TryCompileLikeExpression(bool ignoreCase, Expression corpus, Expression like)
        {
            if (like is ConstantExpression cx &&
                cx.Constant is ScalarValue scalar &&
                scalar.Value is string s)
            {
                var regex = LikeToRegex(s);
                var opts = RegexOptions.Compiled | RegexOptions.ExplicitCapture;
                if (ignoreCase)
                    opts |= RegexOptions.IgnoreCase;
                var compiled = new Regex(regex, opts, TimeSpan.FromMilliseconds(100));
                var indexof = new IndexOfMatchExpression(Transform(corpus), compiled);
                return new CallExpression(ignoreCase, Operators.RuntimeOpNotEqual, indexof, new ConstantExpression(new ScalarValue(-1)));
            }
            
            SelfLog.WriteLine($"Serilog.Expressions: `like` requires a constant string argument; found ${like}.");
            return new CallExpression(false, Operators.OpUndefined);
        }
        
        static string LikeToRegex(string like)
        {
            var regex = "";
            for (var i = 0; i < like.Length; ++i)
            {
                var ch = like[i];
                char? following = i == like.Length - 1 ? (char?)null : like[i + 1];
                if (ch == '%')
                {
                    if (following == '%')
                    {
                        regex += '%';
                        ++i;
                    }
                    else
                    {
                        regex += "(?:.|\\r|\\n)*"; // ~= RegexOptions.Singleline
                    }
                }
                else if (ch == '_')
                {
                    if (following == '_')
                    {
                        regex += '_';
                        ++i;
                    }
                    else
                    {
                        regex += '.'; // Newlines aren't considered matches for _
                    }
                }
                else
                    regex += Regex.Escape(ch.ToString());
            }

            return regex;
        }
    }
}

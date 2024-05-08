using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionPushScope : ParseAction
    {
        public ParseActionPushScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            if (parser.CurrentScope == null) // 全局
            {
                parser.PushScope(new Scope());
            }
            else
            {
                parser.PushScope(new Scope(parserContext.CurrentToken.Line, parserContext.CurrentToken.StartColumn, parser.CurrentScope));
            }
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.PopScope();
        }
    }
}

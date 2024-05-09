using Compiler.Syntax.Model;

namespace Compiler.Syntax.ParseActions
{
    internal class ParseActionPushScope : ParseAction
    {
        public override string FunctionName => "PushScope";

        public ParseActionPushScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            if (m_arguments.Count >= 1)
            {
                var symbolToken = parser.PeekLastSymbolToken(int.Parse(m_arguments[0]));
                parser.PushScope(new Scope(symbolToken, parser.CurrentScope));
            }
            else
            {
                parser.PushScope(new Scope(parserContext.CurrentToken.Line, parserContext.CurrentToken.StartColumn, parser.CurrentScope));
            }
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.RevertScope();
        }
    }
}

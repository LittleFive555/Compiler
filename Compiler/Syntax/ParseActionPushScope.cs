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
            if (m_arguments.Count >= 1)
            {
                var symbolToken = parser.SymbolTable.PeekLastSymbolToken(int.Parse(m_arguments[0]));
                var symbol = parser.SymbolTable.GetSymbol(symbolToken);
                if (symbol == null)
                    throw new Exception();

                parser.PushScope(new Scope(symbol));
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

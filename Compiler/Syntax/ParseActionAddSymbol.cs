using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionAddSymbol : ParseAction
    {
        public ParseActionAddSymbol(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            Symbol symbol = new Symbol(parserContext.CurrentToken, parser.CurrentScope);
            parser.SymbolTable.AddSymbol(symbol);
        }
    }
}

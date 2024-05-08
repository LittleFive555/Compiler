using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionAddSymbol : ParseAction
    {
        private Symbol m_addedSymbol;
        public ParseActionAddSymbol(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_addedSymbol = new Symbol(parserContext.CurrentToken, parser.CurrentScope);
            parser.SymbolTable.AddSymbol(m_addedSymbol);
        }

        public override void RevertExecute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            parser.SymbolTable.RemoveSymbol(m_addedSymbol);
        }
    }
}

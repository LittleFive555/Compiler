using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionAddSymbolDefinition : ParseAction
    {
        private Token m_addedToken;
        private Scope m_scope;

        public ParseActionAddSymbolDefinition(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_addedToken = parserContext.CurrentToken;
            m_scope = parser.CurrentScope;
            parser.SymbolTable.AddSymbolReference(parserContext.CurrentToken, ReferenceType.Defination, parser.CurrentScope);
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.SymbolTable.RemoveSymbolReference(m_addedToken, m_scope);
        }
    }
}

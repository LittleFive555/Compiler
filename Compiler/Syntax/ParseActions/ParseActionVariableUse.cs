using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax.ParseActions
{
    internal class ParseActionVariableUse : ParseAction
    {
        public override string FunctionName => "VariableUse";

        private Token m_addedToken;
        private Scope m_scope;

        public ParseActionVariableUse(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_addedToken = parserContext.CurrentToken;
            m_scope = parser.CurrentScope;
            parser.SymbolTable.AddSymbolReference(parserContext.CurrentToken, ReferenceType.VariableUse, parser.CurrentScope);
            parser.SymbolTable.PushSymbolToken(parserContext.CurrentToken);
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.SymbolTable.RemoveSymbolReference(m_addedToken, m_scope);
            parser.SymbolTable.PopSymbolToken();
        }
    }
}

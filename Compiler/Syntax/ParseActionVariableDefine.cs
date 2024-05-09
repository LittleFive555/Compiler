using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionVariableDefine : ParseAction
    {
        public override string FunctionName => "VariableDefine";

        private Token m_addedToken;
        private Scope m_scope;

        public ParseActionVariableDefine(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_addedToken = parserContext.CurrentToken;
            m_scope = parser.CurrentScope;
            parser.SymbolTable.AddSymbolReference(parserContext.CurrentToken, ReferenceType.VariableDefine, parser.CurrentScope);
            parser.SymbolTable.PushSymbolToken(parserContext.CurrentToken);
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.SymbolTable.RemoveSymbolReference(m_addedToken, m_scope);
            parser.SymbolTable.PopSymbolToken();
        }
    }
}

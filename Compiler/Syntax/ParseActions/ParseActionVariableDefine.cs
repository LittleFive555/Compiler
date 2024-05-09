using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax.ParseActions
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
            m_scope = parser.CurrentFile.CurrentScope;
            parser.CurrentFile.PushSymbolReference(parserContext.CurrentToken, ReferenceType.VariableDefine, parser.CurrentFile.CurrentScope);
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.CurrentFile.PopSymbolReference(m_addedToken, m_scope);
        }
    }
}

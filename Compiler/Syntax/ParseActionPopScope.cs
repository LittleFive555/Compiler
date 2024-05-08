using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class ParseActionPopScope : ParseAction
    {
        private Scope m_poppedScope;

        public ParseActionPopScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_poppedScope = parser.PopScope();
        }

        public override void RevertExecute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            parser.PushScope(m_poppedScope);
        }
    }
}

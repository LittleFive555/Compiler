using Compiler.Syntax.Model;

namespace Compiler.Syntax.ParseActions
{
    internal class ParseActionPopScope : ParseAction
    {
        public override string FunctionName => "PopScope";

        private Scope m_poppedScope;

        public ParseActionPopScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            m_poppedScope = parser.CurrentFile.PopScope();
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.CurrentFile.PushScope(m_poppedScope);
        }
    }
}

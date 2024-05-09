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
                var symbolToken = parser.CurrentFile.PeekLastSymbolToken(int.Parse(m_arguments[0]));
                parser.CurrentFile.PushScope(new Scope(symbolToken, parser.CurrentFile.CurrentScope));
            }
            else
            {
                parser.CurrentFile.PushScope(new Scope(parserContext.FileData.DocumentUri, parserContext.CurrentToken.Line, parserContext.CurrentToken.StartColumn, parser.CurrentFile.CurrentScope));
            }
        }

        public override void RevertExecute(SyntaxAnalyzer parser)
        {
            parser.CurrentFile.RevertScope();
        }
    }
}

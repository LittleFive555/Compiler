namespace Compiler.Syntax
{
    internal class ParseActionPushScope : ParseAction
    {
        public ParseActionPushScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            if (parser.CurrentScope == null) // 全局
            {
                parser.PushScope(new Model.Scope());
            }
            else
            {
                parser.PushScope(new Model.Scope(parserContext.CurrentToken.Line, parserContext.CurrentToken.StartColumn, parser.CurrentScope));
            }
        }
    }
}

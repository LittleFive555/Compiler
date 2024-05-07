namespace Compiler.Syntax
{
    internal class ParseActionPopScope : ParseAction
    {
        public ParseActionPopScope(string content) : base(content)
        {
        }

        public override void Execute(SyntaxAnalyzer parser, ParserContext parserContext)
        {
            parser.PopScope();
        }
    }
}

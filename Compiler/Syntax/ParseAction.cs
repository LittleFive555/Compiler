namespace Compiler.Syntax
{
    internal abstract class ParseAction : SyntaxUnit
    {
        public ParseAction(string content) : base(content)
        {
            SyntaxUnitType = SyntaxUnitType.ParseAction;
        }

        public abstract void Execute(SyntaxAnalyzer parser, ParserContext parserContext);
    }
}

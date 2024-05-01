namespace Compiler.Syntax
{
    public abstract class ParseAction : SyntaxUnit
    {
        public ParseAction(string content) : base(content)
        {
            SyntaxUnitType = SyntaxUnitType.ParseAction;
        }

        public abstract void Execute();
    }
}

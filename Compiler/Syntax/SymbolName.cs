namespace Compiler.Syntax
{
    public class SymbolName : SyntaxUnit
    {
        public SymbolName(string content) : base(content)
        {
            SyntaxUnitType = SyntaxUnitType.SymbolName;
        }
    }
}

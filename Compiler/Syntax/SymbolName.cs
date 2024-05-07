namespace Compiler.Syntax
{
    internal class SymbolName : SyntaxUnit
    {
        public SymbolName(string content) : base(content)
        {
            SyntaxUnitType = SyntaxUnitType.SymbolName;
        }
    }
}

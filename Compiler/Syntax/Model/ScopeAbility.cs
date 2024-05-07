namespace Compiler.Syntax.Model
{
    public enum ScopeAbility
    {
        None = 0b00000000,
        CanBreak = 0b00000001,
        CanContinue = 0b00000010,
        CanCase = 0b00000100,
        CanReturn = 0b00001000,
    }
}

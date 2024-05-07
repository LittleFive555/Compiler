using Compiler.Lexical;

namespace Compiler.Syntax.Model
{
    public class Symbol
    {
        public string Name { get; }

        public string Identifier { get; }

        public int Length { get; }

        public Scope BelongedScope { get; private set; }

        public Symbol(Token token, Scope belongedScope)
        {
            Name = token.Content;
            Length = token.Length;
            BelongedScope = belongedScope;
        }

        public void UpdateScope(Scope scope)
        {
            BelongedScope = scope;
        }
    }
}

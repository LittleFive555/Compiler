using Compiler.Lexical;

namespace Compiler.Syntax.Model
{
    public class SymbolReference : IEquatable<SymbolReference?>
    {
        public Token Token { get; }

        public ReferenceType ReferenceType { get; }

        public SymbolReference(Token token, ReferenceType referenceType)
        {
            Token = token;
            ReferenceType = referenceType;
        }

        public override string ToString()
        {
            return string.Format("{0}:({1},{2})", Token.Document.AbsolutePath, Token.Line, Token.StartColumn);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as SymbolReference);
        }

        public bool Equals(SymbolReference? other)
        {
            return other is not null &&
                   EqualityComparer<Token>.Default.Equals(Token, other.Token);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Token);
        }

        public static bool operator ==(SymbolReference? left, SymbolReference? right)
        {
            return EqualityComparer<SymbolReference>.Default.Equals(left, right);
        }

        public static bool operator !=(SymbolReference? left, SymbolReference? right)
        {
            return !(left == right);
        }
    }
}

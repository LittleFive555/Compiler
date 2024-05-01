namespace Compiler.Syntax
{
    public abstract class SyntaxUnit : IEquatable<SyntaxUnit?>
    {
        internal SyntaxUnitType SyntaxUnitType;

        public string Content;

        public SyntaxUnit(string content)
        {
            Content = content;
        }

        public override bool Equals(object? obj)
        {
            return Content == (obj as SyntaxUnit)?.Content;
        }

        public bool Equals(SyntaxUnit? other)
        {
            return other is not null &&
                   SyntaxUnitType == other.SyntaxUnitType &&
                   Content == other.Content;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SyntaxUnitType, Content);
        }

        public static bool operator ==(SyntaxUnit? left, SyntaxUnit? right)
        {
            return EqualityComparer<SyntaxUnit>.Default.Equals(left, right);
        }

        public static bool operator !=(SyntaxUnit? left, SyntaxUnit? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Content;
        }
    }
}

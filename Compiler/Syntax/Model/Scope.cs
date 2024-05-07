using System.Text;

namespace Compiler.Syntax.Model
{
    public class Scope : IEquatable<Scope?>
    {
        public string Identifier { get; }

        public Scope? ParentScope { get; }

        private int m_scopeAbility;

        public Scope(Symbol symbol, params ScopeAbility[] scopeAbilities) : this(scopeAbilities)
        {
            var parentScope = symbol.BelongedScope;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(parentScope.Identifier);
            stringBuilder.Append(", ");
            stringBuilder.Append(symbol.Name);
            Identifier = stringBuilder.ToString();

            ParentScope = parentScope;
        }

        public Scope(int startLine, int startColumn, Scope parentScope, params ScopeAbility[] scopeAbilities) : this(scopeAbilities)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(parentScope.Identifier);
            stringBuilder.Append(", ");
            stringBuilder.Append(string.Format("({0},{1})", startLine, startColumn));
            Identifier = stringBuilder.ToString();

            ParentScope = parentScope;
        }

        public Scope(params ScopeAbility[] scopeAbilities)
        {
            Identifier = "Global";

            ParentScope = null;

            m_scopeAbility = (int)ScopeAbility.None;
            foreach (var ability in scopeAbilities)
                m_scopeAbility |= (int)ability;
        }

        public bool HaveAbility(ScopeAbility scopeAbility)
        {
            if ((m_scopeAbility & (int)scopeAbility) != 0)
                return true;
            else if (ParentScope != null)
                return ParentScope.HaveAbility(scopeAbility);
            else
                return false;
        }

        public bool IsParentOf(Scope scope)
        {
            return scope.Identifier.StartsWith(Identifier);
        }

        public bool IsChildOf(Scope scope)
        {
            return Identifier.StartsWith(scope.Identifier);
        }

        public override string ToString()
        {
            return Identifier;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Scope);
        }

        public bool Equals(Scope? other)
        {
            return other is not null &&
                   Identifier == other.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }

        public static bool operator ==(Scope? left, Scope? right)
        {
            return left?.Identifier == right?.Identifier;
        }

        public static bool operator !=(Scope? left, Scope? right)
        {
            return !(left == right);
        }
    }
}

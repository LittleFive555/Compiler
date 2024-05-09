using Compiler.Lexical;
using System.Text;

namespace Compiler.Syntax.Model
{
    public class Scope : IEquatable<Scope?>
    {
        public string Identifier { get; }

        public Scope? ParentScope { get; }

        public List<Scope> Children = new List<Scope>();

        public Dictionary<string, List<SymbolReference>> References { get; } = new Dictionary<string, List<SymbolReference>>();
        private List<SymbolReference> m_referencesList = new List<SymbolReference>();

        public int Level { get; }

        private int m_scopeAbility;

        public Scope(Token token, Scope parentScope, params ScopeAbility[] scopeAbilities) : this(scopeAbilities)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(parentScope.Identifier);
            stringBuilder.Append(", ");
            stringBuilder.Append(token.Content);
            Identifier = stringBuilder.ToString();

            ParentScope = parentScope;
            ParentScope.PushChild(this);

            Level = parentScope.Level + 1;
        }

        public Scope(int startLine, int startColumn, Scope parentScope, params ScopeAbility[] scopeAbilities) : this(scopeAbilities)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(parentScope.Identifier);
            stringBuilder.Append(", ");
            stringBuilder.Append(string.Format("({0},{1})", startLine, startColumn));
            Identifier = stringBuilder.ToString();

            ParentScope = parentScope;
            ParentScope.PushChild(this);

            Level = parentScope.Level + 1;
        }

        public Scope(params ScopeAbility[] scopeAbilities)
        {
            Identifier = "Global";

            ParentScope = null;

            m_scopeAbility = (int)ScopeAbility.None;
            foreach (var ability in scopeAbilities)
                m_scopeAbility |= (int)ability;

            Level = 0;
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

        public void PushChild(Scope childScope)
        {
            Children.Add(childScope);
        }

        public void PopChild()
        {
            Children.RemoveAt(Children.Count - 1);
        }

        public bool IsParentOf(Scope scope)
        {
            return scope.Identifier.StartsWith(Identifier) && scope.Identifier != Identifier;
        }

        public bool IsChildOf(Scope scope)
        {
            return Identifier.StartsWith(scope.Identifier) && scope.Identifier != Identifier;
        }

        public void PushSymbolReference(SymbolReference symbolReference)
        {
            string symbolName = symbolReference.Token.Content;
            if (!References.ContainsKey(symbolName))
                References.Add(symbolName, new List<SymbolReference>());
            References[symbolName].Add(symbolReference);

            m_referencesList.Add(symbolReference);
        }

        public SymbolReference PopSymbolReference()
        {
            var removed = m_referencesList[m_referencesList.Count - 1];
            string symbolName = removed.Token.Content;
            References[symbolName].RemoveAt(References[symbolName].Count - 1);
            m_referencesList.Remove(removed);
            return removed;
        }

        #region override

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

        #endregion
    }
}

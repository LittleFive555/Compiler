using Compiler.Lexical;
using System.Text;

namespace Compiler.Syntax.Model
{
    public class Symbol
    {
        public string Name { get; }

        public string Identifier { get; }

        public int Length => Name.Length;

        public Scope BelongedScope { get; private set; }

        private HashSet<SymbolReference> m_references = new HashSet<SymbolReference>();

        public Symbol(string name, Scope belongedScope)
        {
            Name = name;
            BelongedScope = belongedScope;
        }

        public void AddReferences(HashSet<SymbolReference> symbolReferences)
        {
            m_references.UnionWith(symbolReferences);
        }

        public IReadOnlySet<SymbolReference> GetReferences()
        {
            return m_references;
        }

        public bool IsOneOfReference(Token token)
        {
            // XXX 这里随便传入一个ReferenceType，因为该变量不参与值比较
            return m_references.Contains(new SymbolReference(token, ReferenceType.VariableUse, null));
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Name:");
            stringBuilder.Append(Name);
            stringBuilder.Append(", ");
            stringBuilder.Append("BelongedScope:");
            stringBuilder.Append(BelongedScope);
            stringBuilder.Append(", ");
            stringBuilder.Append("References:");
            foreach (var symbolReference in m_references)
                stringBuilder.AppendLine(symbolReference.ToString());
            return stringBuilder.ToString();
        }
    }
}

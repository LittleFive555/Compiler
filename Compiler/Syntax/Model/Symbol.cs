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

        private Dictionary<Uri, HashSet<SymbolReference>> m_references = new Dictionary<Uri, HashSet<SymbolReference>>();

        public Symbol(string name, Scope belongedScope)
        {
            Name = name;
            BelongedScope = belongedScope;
        }

        public void AddReferences(Uri uri, HashSet<SymbolReference> symbolReferences)
        {
            m_references.Add(uri, symbolReferences);
        }

        public IReadOnlyList<SymbolReference> GetReferences()
        {
            List<SymbolReference> result = new List<SymbolReference>();
            foreach (var reference in m_references.Values)
                result.AddRange(reference);
            return result;
        }

        public bool IsOneOfReference(Token token)
        {
            if (!m_references.ContainsKey(token.Document))
                return false;

            // XXX 这里随便传入一个ReferenceType，因为该变量不参与值比较
            return m_references[token.Document].Contains(new SymbolReference(token, ReferenceType.VariableUse, null));
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
            foreach (var fileReferences in m_references.Values)
            {
                foreach (var symbolReference in fileReferences)
                    stringBuilder.AppendLine(symbolReference.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}

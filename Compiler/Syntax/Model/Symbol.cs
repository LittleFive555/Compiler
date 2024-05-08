using Compiler.Lexical;
using System.Text;

namespace Compiler.Syntax.Model
{
    public class Symbol
    {
        public string Name { get; }

        public string Identifier { get; }

        public int Length { get; }

        public Scope BelongedScope { get; private set; }

        private Dictionary<Uri, HashSet<SymbolReference>> m_references = new Dictionary<Uri, HashSet<SymbolReference>>();

        public Symbol(Token token, ReferenceType referenceType,Scope belongedScope)
        {
            Name = token.Content;
            Length = token.Length;
            BelongedScope = belongedScope;
            AddReference(token, referenceType);
        }

        public void UpdateScope(Scope scope)
        {
            BelongedScope = scope;
        }

        public void AddReference(Token token, ReferenceType referenceType)
        {
            var uri = token.Document;
            if (!m_references.ContainsKey(uri))
                m_references.Add(uri, new HashSet<SymbolReference>());
            m_references[uri].Add(new SymbolReference(token, referenceType));
        }

        public void RemoveReference(Token token)
        {
            // XXX 这里随便传入一个ReferenceType，因为该变量不参与值比较
            m_references[token.Document].Remove(new SymbolReference(token, ReferenceType.Defination));
            if (m_references[token.Document].Count == 0)
                m_references.Remove(token.Document);
        }

        public bool HaveReferences()
        {
            return GetReferences().Count > 0;
        }

        public IReadOnlyList<SymbolReference> GetReferences()
        {
            List<SymbolReference> result = new List<SymbolReference>();
            foreach (var reference in m_references.Values)
                result.AddRange(reference);
            return result;
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

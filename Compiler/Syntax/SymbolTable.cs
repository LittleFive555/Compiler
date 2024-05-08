using Compiler.Lexical;
using Compiler.Syntax.Model;
using System.Text;

namespace Compiler.Syntax
{
    public class SymbolTable
    {
        private Dictionary<Scope, ISet<Symbol>> m_symbols = new Dictionary<Scope, ISet<Symbol>>();

        private List<Token> m_lastSymbolTokenStack = new List<Token>();
        private int m_maxCount = 50;

        public void AddSymbolReference(Token token, ReferenceType referenceType, Scope belongedScope)
        {
            if (!m_symbols.ContainsKey(belongedScope))
                m_symbols.Add(belongedScope, new HashSet<Symbol>());
            if (m_symbols[belongedScope].Any((symbol) => symbol.BelongedScope == belongedScope && symbol.Name == token.Content))
            {
                var existSymbol = m_symbols[belongedScope].Single((symbol) => symbol.BelongedScope == belongedScope && symbol.Name == token.Content);
                existSymbol.AddReference(token, referenceType);
            }
            else
            {
                m_symbols[belongedScope].Add(new Symbol(token, referenceType, belongedScope));
            }
        }

        public void RemoveSymbolReference(Token token, Scope belongedScope)
        {
            if (!m_symbols.ContainsKey(belongedScope))
                return;

            if (!m_symbols[belongedScope].Any((symbol) => symbol.BelongedScope == belongedScope && symbol.Name == token.Content))
                return;

            var existSymbol = m_symbols[belongedScope].Single((symbol) => symbol.BelongedScope == belongedScope && symbol.Name == token.Content);
            existSymbol.RemoveReference(token);
            if (!existSymbol.HaveReferences())
                m_symbols[belongedScope].Remove(existSymbol);
            return;
        }

        public IReadOnlyList<SymbolReference>? GetSymbolReferences(Token token)
        {
            foreach (var symbolsByScope in  m_symbols.Values)
            {
                if (symbolsByScope.Any((symbol) => symbol.IsOneOfReference(token)))
                    return symbolsByScope.Single((symbol) => symbol.IsOneOfReference(token)).GetReferences();
            }
            return null;
        }

        public Symbol? GetSymbol(Token token)
        {
            foreach (var symbolsByScope in m_symbols.Values)
            {
                if (symbolsByScope.Any((symbol) => symbol.IsOneOfReference(token)))
                    return symbolsByScope.Single((symbol) => symbol.IsOneOfReference(token));
            }
            return null;
        }

        public Token PeekLastSymbolToken(int count)
        {
            if (count > m_lastSymbolTokenStack.Count)
                throw new IndexOutOfRangeException();

            return m_lastSymbolTokenStack[m_lastSymbolTokenStack.Count - count];
        }

        public void PushSymbolToken(Token token)
        {
            m_lastSymbolTokenStack.Add(token);
            if (m_lastSymbolTokenStack.Count > m_maxCount * 2)
                m_lastSymbolTokenStack.RemoveAt(0);
        }

        public void PopSymbolToken()
        {
            if (m_lastSymbolTokenStack.Count == 0)
                return;

            m_lastSymbolTokenStack.RemoveAt(m_lastSymbolTokenStack.Count - 1);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var symbolSet in  m_symbols.Values)
            {
                foreach (var symbol in symbolSet)
                {
                    stringBuilder.Append(symbol.ToString());
                    stringBuilder.AppendLine();
                }
            }
            return stringBuilder.ToString();
        }
    }
}

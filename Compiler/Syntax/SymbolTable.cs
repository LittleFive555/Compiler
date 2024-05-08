using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    public class SymbolTable
    {
        private Dictionary<Scope, ISet<Symbol>> m_symbols = new Dictionary<Scope, ISet<Symbol>>();

        public void AddSymbol(Symbol symbol)
        {
            if (!m_symbols.ContainsKey(symbol.BelongedScope))
                m_symbols.Add(symbol.BelongedScope, new HashSet<Symbol>());
            m_symbols[symbol.BelongedScope].Add(symbol);
        }

        public void RemoveSymbol(Symbol symbol)
        {
            if (m_symbols.ContainsKey(symbol.BelongedScope))
                m_symbols[symbol.BelongedScope].Remove(symbol);
        }
    }
}

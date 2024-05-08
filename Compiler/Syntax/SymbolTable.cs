﻿using Compiler.Lexical;
using Compiler.Syntax.Model;
using System.Text;

namespace Compiler.Syntax
{
    public class SymbolTable
    {
        private Dictionary<Scope, ISet<Symbol>> m_symbols = new Dictionary<Scope, ISet<Symbol>>();

        public void AddSymbol(Token token, ReferenceType referenceType, Scope belongedScope)
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

        public void RemoveSymbol(Token token, Scope belongedScope)
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

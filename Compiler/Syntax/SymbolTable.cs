using Compiler.Lexical;
using Compiler.Syntax.Model;
using System.Text;

namespace Compiler.Syntax
{
    public class SymbolTable
    {
        private Dictionary<string, Dictionary<Scope, Symbol>> m_symbols = new Dictionary<string, Dictionary<Scope, Symbol>>();

        private Scope m_rootScope;

        public SymbolTable(Scope rootScope)
        {
            m_rootScope = rootScope;
        }

        public void CollectSymbols()
        {
            Dictionary<string, Dictionary<Scope, List<SymbolReference>>> referenceByScope = new Dictionary<string, Dictionary<Scope, List<SymbolReference>>>();
            CollectSymbols(m_rootScope, referenceByScope);
            
            foreach (var reference in referenceByScope)
            {
                m_symbols.Add(reference.Key, new Dictionary<Scope, Symbol>());
                foreach (var symbol in reference.Value)
                {
                    Symbol ssss = new Symbol(reference.Key, symbol.Key);
                    ssss.AddReferences(symbol.Value[0].Token.Document, new HashSet<SymbolReference>(symbol.Value));
                    m_symbols[reference.Key].Add(symbol.Key, ssss);
                }
            }
        }

        private void CollectSymbols(Scope scopePointer, Dictionary<string, Dictionary<Scope, List<SymbolReference>>> symbols)
        {
            foreach (var symbolReference in scopePointer.References)
            {
                string symbolName = symbolReference.Key;
                if (!symbols.ContainsKey(symbolName))
                    symbols.Add(symbolName, new Dictionary<Scope, List<SymbolReference>>());
                Dictionary<Scope, List<SymbolReference>> symbolDic = symbols[symbolName];
                foreach (var reference in symbolReference.Value)
                {
                    SetReferenceBelongedScope(scopePointer, symbolDic, reference);
                }
            }

            foreach (var childScope in scopePointer.Children)
            {
                CollectSymbols(childScope, symbols);
            }
        }

        private void SetReferenceBelongedScope(Scope currentScope, Dictionary<Scope, List<SymbolReference>> symbolDic, SymbolReference reference)
        {
            if (reference.ReferenceType == ReferenceType.TypeDefine || reference.ReferenceType == ReferenceType.VariableDefine)
            {
                if (symbolDic.ContainsKey(currentScope))
                    symbolDic[currentScope].Add(reference);
                else
                    symbolDic.Add(currentScope, new List<SymbolReference>() { reference });
            }
            else
            {
                Scope? tempScope = currentScope;
                while (tempScope != null)
                {
                    if (symbolDic.ContainsKey(tempScope))
                        break;
                    tempScope = tempScope.ParentScope;
                }

                if (tempScope == null)
                    symbolDic.Add(m_rootScope, new List<SymbolReference>() { reference });
                else
                    symbolDic[tempScope].Add(reference);
            }
        }

        public IReadOnlyList<SymbolReference>? GetSymbolReferences(Token token)
        {
            var symbol = GetSymbol(token);
            return symbol?.GetReferences();
        }

        public Symbol? GetSymbol(Token token)
        {
            if (!m_symbols.ContainsKey(token.Content))
                return null;

            foreach (var symbol in m_symbols[token.Content].Values)
            {
                if (symbol.IsOneOfReference(token))
                    return symbol;
            }
            return null;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var symbolSet in  m_symbols.Values)
            {
                foreach (var symbol in symbolSet.Values)
                {
                    stringBuilder.Append(symbol.ToString());
                    stringBuilder.AppendLine();
                }
            }
            return stringBuilder.ToString();
        }
    }
}

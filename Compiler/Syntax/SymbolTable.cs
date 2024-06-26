﻿using System.Text;
using Compiler.Lexical;
using Compiler.Syntax.Model;

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

        public static SymbolTable Merge(params SymbolTable[] symbolTables)
        {
            SymbolTable result = new SymbolTable(new Scope());
            foreach (var symbolTable in symbolTables)
            {
                foreach (var keyValue in symbolTable.m_symbols)
                {
                    if (!result.m_symbols.ContainsKey(keyValue.Key))
                        result.m_symbols.Add(keyValue.Key, new Dictionary<Scope, Symbol>());

                    var symbolDic = result.m_symbols[keyValue.Key];
                    foreach (var symbol in keyValue.Value)
                    {
                        if (!symbolDic.ContainsKey(symbol.Key))
                            symbolDic.Add(symbol.Key, new Symbol(symbol.Value.Name, symbol.Value.BelongedScope));
                        symbolDic[symbol.Key].AddReferences(symbol.Value.GetReferences());
                    }
                }
            }
            return result;
        }

        public void Remove(SymbolTable toRemove)
        {
            List<string> emptyStrings = new List<string>();
            foreach (var keyValue in toRemove.m_symbols)
            {
                if (!m_symbols.ContainsKey(keyValue.Key))
                    continue;

                var symbolDic = m_symbols[keyValue.Key];
                List<Scope> emptyScopes = new List<Scope>();
                foreach (var symbol in keyValue.Value)
                {
                    if (!symbolDic.ContainsKey(symbol.Key))
                        continue;

                    symbolDic[symbol.Key].RemoveReferences(symbol.Value.GetReferences());
                    if (symbolDic[symbol.Key].GetReferences().Count == 0)
                        emptyScopes.Add(symbol.Key);
                }
                foreach (var emptyScope in emptyScopes)
                    symbolDic.Remove(emptyScope);

                if (symbolDic.Count == 0)
                    emptyStrings.Add(keyValue.Key);
            }
            foreach (var emptyString in emptyStrings)
                m_symbols.Remove(emptyString);
        }

        public IReadOnlySet<SymbolReference>? GetSymbolReferences(Token token)
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

        internal void CollectSymbols()
        {
            var referencesByScope = CollectReferencesByScope(m_rootScope);

            foreach (var allReferences in referencesByScope)
            {
                m_symbols.Add(allReferences.Key, new Dictionary<Scope, Symbol>());
                foreach (var symbolReferences in allReferences.Value)
                {
                    Symbol symbol = new Symbol(allReferences.Key, symbolReferences.Key);
                    symbol.AddReferences(symbolReferences.Value);
                    m_symbols[allReferences.Key].Add(symbolReferences.Key, symbol);
                }
            }
        }

        private Dictionary<string, Dictionary<Scope, HashSet<SymbolReference>>> CollectReferencesByScope(Scope scopePointer)
        {
            Dictionary<string, Dictionary<Scope, HashSet<SymbolReference>>> referencesByScope = new Dictionary<string, Dictionary<Scope, HashSet<SymbolReference>>>();
            CollectReferencesByScopeImpl(scopePointer, referencesByScope);
            return referencesByScope;
        }

        private void CollectReferencesByScopeImpl(Scope scopePointer, Dictionary<string, Dictionary<Scope, HashSet<SymbolReference>>> referencesByScope)
        {
            foreach (var symbolReference in scopePointer.References)
            {
                string symbolName = symbolReference.Key;
                if (!referencesByScope.ContainsKey(symbolName))
                    referencesByScope.Add(symbolName, new Dictionary<Scope, HashSet<SymbolReference>>());
                foreach (var reference in symbolReference.Value)
                    SetReferenceToScope(reference, scopePointer, referencesByScope[symbolName]);
            }

            foreach (var childScope in scopePointer.Children)
                CollectReferencesByScopeImpl(childScope, referencesByScope);
        }

        private void SetReferenceToScope(SymbolReference reference, Scope currentScope, Dictionary<Scope, HashSet<SymbolReference>> symbolDic)
        {
            if (reference.ReferenceType == ReferenceType.TypeDefine || reference.ReferenceType == ReferenceType.VariableDefine)
            {
                if (symbolDic.ContainsKey(currentScope))
                    symbolDic[currentScope].Add(reference);
                else
                    symbolDic.Add(currentScope, new HashSet<SymbolReference>() { reference });
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
                    symbolDic.Add(m_rootScope, new HashSet<SymbolReference>() { reference });
                else
                    symbolDic[tempScope].Add(reference);
            }
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

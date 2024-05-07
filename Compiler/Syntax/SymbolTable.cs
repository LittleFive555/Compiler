using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    public class SymbolTable
    {
        private Dictionary<Scope, ISet<Symbol>> m_symbols = new Dictionary<Scope, ISet<Symbol>>();
    }
}

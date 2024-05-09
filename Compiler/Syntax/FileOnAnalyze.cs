using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class FileOnAnalyze
    {
        public FileData FileData { get; }
        public Scope CurrentScope => m_scopeStack.Peek();

        private Scope m_rootScope = new Scope();
        private Stack<Scope> m_scopeStack = new Stack<Scope>();
        private SymbolTable m_symbolTable;

        public FileOnAnalyze(FileData fileData)
        {
            FileData = fileData;
            PushScope(m_rootScope);
            m_symbolTable = new SymbolTable(m_rootScope);
        }

        public void PushScope(Scope scope)
        {
            m_scopeStack.Push(scope);
        }

        /// <summary>
        /// 用于分析正常过程中的作用域进出
        /// </summary>
        public Scope PopScope()
        {
            var lastScope = m_scopeStack.Pop();
            return lastScope;
        }

        /// <summary>
        /// 用于回溯
        /// </summary>
        public Scope RevertScope()
        {
            var lastScope = m_scopeStack.Pop();
            if (lastScope.ParentScope != null)
                lastScope.ParentScope.PopChild();
            return lastScope;
        }

        private List<Token> m_lastSymbolTokenStack = new List<Token>();
        private int m_maxCount = 50;

        public void PushSymbolReference(Token token, ReferenceType referenceType, Scope currentScope)
        {
            m_lastSymbolTokenStack.Add(token);
            if (m_lastSymbolTokenStack.Count > m_maxCount * 2)
                m_lastSymbolTokenStack.RemoveAt(0);

            var newReference = new SymbolReference(token, referenceType, currentScope);
            currentScope.PushSymbolReference(newReference);
        }

        public void PopSymbolReference(Token token, Scope currentScope)
        {
            m_lastSymbolTokenStack.RemoveAt(m_lastSymbolTokenStack.Count - 1);

            currentScope.PopSymbolReference();
        }

        public Token PeekLastSymbolToken(int count)
        {
            if (count > m_lastSymbolTokenStack.Count)
                throw new IndexOutOfRangeException();

            return m_lastSymbolTokenStack[m_lastSymbolTokenStack.Count - count];
        }

        public void GenerateSymbolTable()
        {
            m_symbolTable.CollectSymbols();
        }

        public SymbolTable GetSymbolTable()
        {
            return m_symbolTable;
        }
    }
}

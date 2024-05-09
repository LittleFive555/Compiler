using Compiler.Lexical;
using Compiler.Syntax;

namespace Compiler
{
    public class FileData : IEquatable<FileData?>
    {
        public Uri DocumentUri { get; }

        private List<Token> m_tokenList;
        public IReadOnlyList<Token> TokenList => m_tokenList;

        public SymbolTable SymbolTable { get; private set; }

        public FileData(Uri documentUri)
        {
            DocumentUri = documentUri;
        }

        public void SetTokenLine(List<Token> tokens)
        {
            m_tokenList = tokens;
        }

        internal void SetSymbolTable(SymbolTable symbolTable)
        {
            SymbolTable = symbolTable;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FileData);
        }

        public bool Equals(FileData? other)
        {
            return other is not null &&
                   EqualityComparer<Uri>.Default.Equals(DocumentUri, other.DocumentUri);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DocumentUri);
        }

        public static bool operator ==(FileData? left, FileData? right)
        {
            return EqualityComparer<FileData>.Default.Equals(left, right);
        }

        public static bool operator !=(FileData? left, FileData? right)
        {
            return !(left == right);
        }
    }
}

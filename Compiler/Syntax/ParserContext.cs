using Compiler.Lexical;

namespace Compiler.Syntax
{
    internal class ParserContext
    {
        public FileData FileData { get; }

        public Token CurrentToken { get; }

        public ParserContext(FileData fileData, Token currentToken)
        {
            FileData = fileData;
            CurrentToken = currentToken;
        }
    }
}

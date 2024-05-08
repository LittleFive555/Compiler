using Compiler.Lexical;

namespace Compiler.Syntax
{
    internal class ParserContext
    {
        public Uri DocumentUri { get; }

        public Token CurrentToken { get; }

        public ParserContext(Uri documentUri, Token currentToken)
        {
            DocumentUri = documentUri;
            CurrentToken = currentToken;
        }
    }
}

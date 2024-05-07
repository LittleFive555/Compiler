using Compiler.Lexical;

namespace Compiler.Syntax
{
    internal class ParserContext
    {
        public Token CurrentToken { get; }

        public ParserContext(Token currentToken)
        {
            CurrentToken = currentToken;
        }
    }
}

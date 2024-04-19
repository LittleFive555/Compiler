namespace Compiler.Lexical
{
    public class Token
    {
        public string Content { get; }
        public LexicalUnit LexicalUnit { get; }
        public int Line { get; }
        public int StartColumn { get; }
        public int Length { get; }

        public Token(string content, LexicalUnit lexicalUnit, int line, int startColumn, int length)
        {
            Content = content;
            LexicalUnit = lexicalUnit;
            Line = line;
            StartColumn = startColumn;
            Length = length;
        }

        public override string ToString()
        {
            return string.Format("Content: {0}, LexicalUnitName: {1}, Line: {2}, Column:{3}, Length: {4}", Content, LexicalUnit.Name, Line, StartColumn, Length);
        }
    }
}

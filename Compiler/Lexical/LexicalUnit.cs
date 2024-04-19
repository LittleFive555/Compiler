namespace Compiler.Lexical
{
    public class LexicalUnit
    {
        public string Name;
        public LexicalType LexicalType;
        public int Priority;

        public LexicalUnit(string name, LexicalType lexicalType, int priority)
        {
            Name = name;
            LexicalType = lexicalType;
            Priority = priority;
        }
    }
}

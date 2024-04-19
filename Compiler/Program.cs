namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MyCompiler myCompiler = new MyCompiler(new string[]
            {
                "E:\\SourceCode\\Compiler\\File\\LexerDefine.txt",
                "E:\\SourceCode\\Compiler\\File\\ReservedWord.txt",
                "E:\\SourceCode\\Compiler\\File\\Symbols.txt"
            },
            "E:\\SourceCode\\Compiler\\File\\SyntaxDefine.txt");
            myCompiler.Analyze("E:\\SourceCode\\Compiler\\ScorpioScript\\Test1.sco");
        }
    }
}

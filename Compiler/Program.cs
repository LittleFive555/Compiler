namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MyCompiler myCompiler = new MyCompiler(
                "E:\\SourceCode\\Compiler\\File\\Symbols.txt",
                "E:\\SourceCode\\Compiler\\File\\ReservedWord.txt",
                "E:\\SourceCode\\Compiler\\File\\LexerDefine.txt",
                "E:\\SourceCode\\Compiler\\File\\SyntaxDefine.txt");
            var result = myCompiler.Analyze("E:\\SourceCode\\Compiler\\ScorpioScript\\Test1.sco");
            foreach (var error in result.CompileErrors)
                Console.WriteLine(error);
        }
    }
}

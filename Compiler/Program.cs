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
            var result = myCompiler.Analyze("E:\\Dragonscapes\\Client\\Assets\\ScriptScorpio\\Game\\UI\\UIDragonMerchant\\UIDragonMerchantStore.sco");
            foreach (var error in result.CompileErrors)
                MyLogger.WriteLine(error);
        }
    }
}

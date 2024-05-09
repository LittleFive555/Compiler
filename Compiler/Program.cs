using System.Text;
using Compiler.Syntax;

namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var fileStream = new FileStream("C:\\Users\\dd\\Desktop\\Output.txt", FileMode.Create,  FileAccess.Write);
            var encoding = Encoding.GetEncoding(Encoding.UTF8.CodePage);
            var standardOutput = new StreamWriter(fileStream, encoding) { AutoFlush = true };
            Console.SetOut(standardOutput);

            MyCompiler myCompiler = new MyCompiler(
                "E:\\SourceCode\\Compiler\\File\\Symbols.txt",
                "E:\\SourceCode\\Compiler\\File\\ReservedWord.txt",
                "E:\\SourceCode\\Compiler\\File\\LexerDefine.txt",
                "E:\\SourceCode\\Compiler\\File\\SyntaxDefine2.txt");
            var result1 = myCompiler.Analyze("E:\\SourceCode\\Compiler\\ScorpioScript\\Test1.sco");
            foreach (var error in result1.CompileErrors)
                MyLogger.WriteLine(error);
            var result2 = myCompiler.Analyze("E:\\SourceCode\\Compiler\\ScorpioScript\\Test2.sco");
            foreach (var error in result2.CompileErrors)
                MyLogger.WriteLine(error);
            SymbolTable symbolTable = SymbolTable.Merge(result1.FileData.SymbolTable, result2.FileData.SymbolTable);
            MyLogger.WriteLine(symbolTable);
        }
    }
}

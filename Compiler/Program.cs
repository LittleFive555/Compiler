using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Text.Unicode;

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
                "E:\\SourceCode\\Compiler\\File\\SyntaxDefine1.txt");
            var result = myCompiler.Analyze("E:\\Dragonscapes\\Client\\Assets\\ScriptScorpio\\Game\\UI\\UIDragonMerchant\\UIDragonMerchantStore.sco");
            foreach (var error in result.CompileErrors)
                MyLogger.WriteLine(error);
        }
    }
}

using System.Text;
using Compiler.Lexical;
using Compiler.Syntax;

namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // XXX 需要先读取自定义的词法正则表达式，再读取保留字和特殊符号，因为有判断优先级的问题
            //     或者通过显示传参的方式控制优先级问题
            List<LexicalRegex> allLexicalRegex = LexicalRegexLoader.ReadRegexFromMultiFiles(
                "E:\\SourceCode\\Compiler\\File\\LexerDefine.txt",
                "E:\\SourceCode\\Compiler\\File\\ReservedWord.txt",
                "E:\\SourceCode\\Compiler\\File\\Symbols.txt");

            FA nfa = Regex2NFA.Execute(allLexicalRegex.ToArray());
            //Console.WriteLine("NFA:");
            //Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            //Console.WriteLine("DFA:");
            //Console.WriteLine(dfa);
            var syntaxLines = SyntaxReader.ReadFromFile("E:\\SourceCode\\Compiler\\File\\SyntaxDefine.txt");
            SyntaxAnalyzer syntaxAnalyzer = new SyntaxAnalyzer(syntaxLines);

            var tokens = LexicalAnalyzer.Read(File.OpenRead("E:\\SourceCode\\Compiler\\ScorpioScript\\Test1.sco"), dfa);
            foreach (var token in tokens)
                Console.WriteLine(string.Format("Content: {0}, LexicalUnitName: {1}, TokenLength: {2}", token.Content, token.LexicalUnit.Name, token.Length));
            syntaxAnalyzer.Execute(tokens);
        }

        /// <summary>
        /// 由正则表达式构建NFA，再转换为DFA
        /// </summary>
        private static void Test1()
        {
            var nfa = Regex2NFA.Execute(new LexicalRegex()
            {
                Name = "Letter",
                RegexContent = "A|B|C|D|E|F|G|H|I|J|K|L|M|N|O|P|Q|R|S|T|U|V|W|X|Y|Z|a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z",
                Priority = 2
            });
            Console.WriteLine("NFA:");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine("DFA:");
            Console.WriteLine(dfa);
        }

        /// <summary>
        /// 使用DFA进行判断
        /// </summary>
        private static void Test2()
        {
            var nfa = Regex2NFA.Execute(
                new LexicalRegex()
                {
                    Name = "Digit",
                    RegexContent = "(0|1)+",
                    Priority = 1
                });
            Console.WriteLine("NFA:");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine("DFA:");
            Console.WriteLine(dfa);

            string input = "100";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var result = LexicalAnalyzer.Read(stream, dfa);
            foreach (var token in result)
                Console.WriteLine(string.Format("Content: {0}, LexicalUnitName: {1}, TokenLength: {2}", token.Content, token.LexicalUnit.Name, token.Length));
        }

        /// <summary>
        /// 在语言使用的字符与正则运算符相同时，使用转义符的情况
        /// </summary>
        private static void Test3()
        {
            var nfa = Regex2NFA.Execute(new LexicalRegex()
            {
                Name = "InclusiveOr",
                RegexContent = "\\|\\|",
                Priority = 2
            });
            Console.WriteLine("NFA:");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine("DFA:");
            Console.WriteLine(dfa);
        }
    }
}

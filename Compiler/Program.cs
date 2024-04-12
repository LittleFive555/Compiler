using System.Text;

namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Test4();
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
                    Name = Helpers.WhitespaceName,
                    RegexContent = Helpers.WhitespaceRegex,
                    Priority = 0
                },
                new LexicalRegex()
                {
                    Name = "Number",
                    RegexContent = "0|1|2|3|4|5|6|7|8|9",
                    Priority = 1
                });
            Console.WriteLine("NFA:");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine("DFA:");
            Console.WriteLine(dfa);

            string input = "1  2    3 42\r\n55 9";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var result = Lexical.Read(stream, dfa);
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

        private static void Test4()
        {
            List<LexicalRegex> allLexicalRegex = LexicalRegexLoader.ReadRegexFromMultiFiles(
                "E:\\SourceCode\\Compiler\\File\\ReservedWord.txt",
                "E:\\SourceCode\\Compiler\\File\\Symbols.txt",
                "E:\\SourceCode\\Compiler\\File\\LexerDefine.txt");

            FA nfa = Regex2NFA.Execute(allLexicalRegex.ToArray());
            Console.WriteLine("NFA:");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine("DFA:");
            Console.WriteLine(dfa);
        }
    }
}

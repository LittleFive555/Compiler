using System.Text;

namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var nfa1 = Regex2NFA.Execute("Number", "0|1|2|3|4|5|6|7|8|9", 1);
            Console.WriteLine(nfa1);
            var dfa1 = NFA2DFA.Execute(nfa1);
            Console.WriteLine(dfa1);

            string input = "12342";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var result = Lexical.Read(stream, dfa1);
            foreach (var token in result)
                Console.WriteLine(string.Format("Content: {0}, LexicalUnitName: {1}, TokenLength: {2}", token.Content, token.LexicalUnit.Name, token.Length));

            //var nfa2 = Regex2NFA.Execute("Letter", "A|B|C|D|E|F|G|H|I|J|K|L|M|N|O|P|Q|R|S|T|U|V|W|X|Y|Z|a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z", 2);
            //Console.WriteLine(nfa2);
            //var dfa2 = NFA2DFA.Execute(nfa2);
            //Console.WriteLine(dfa2);
        }
    }
}

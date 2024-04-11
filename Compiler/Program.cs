namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var nfa = Regex2NFA.Execute("0|1|2|3|4|5|6|7|8|9");
            Console.WriteLine(nfa);
            var dfa = NFA2DFA.Execute(nfa);
            Console.WriteLine(dfa);
        }
    }
}

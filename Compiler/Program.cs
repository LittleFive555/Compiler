namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //RegexToNFA("ab(cde)");
            var post = Regex2NFA.Regex2Post("(ab)*");
            Console.WriteLine(post);
            if (post != null)
            {
                var nfa = Regex2NFA.Post2NFA(post);
                OutputNFA(nfa);
            }
        }

        private static void OutputNFA(Regex2NFA.NFA? nfa)
        {
            if (nfa == null)
                return;

            foreach (var line in nfa.Value.Lines)
            {
                foreach (var line1 in line.Value)
                {
                    Console.WriteLine(line1);
                }
            }
        }
    }
}

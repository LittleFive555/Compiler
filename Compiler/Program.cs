namespace Compiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //RegexToNFA("ab(cde)");
            var post = Regex2NFA.Regex2Post("abc");
            Console.WriteLine(post);
            if (post != null)
            {
                var startState = Regex2NFA.Post2NFA(post);
                OutputNFA(startState);
            }
        }

        private static void OutputNFA(Regex2NFA.State state)
        {
            if (state == null)
                return;

            if (state.Output != null)
            {
                Console.WriteLine("{0}--{1}-->{2}", state.Id, state.Char, state.Output.Id);
                OutputNFA(state.Output);
            }
            if (state.Output1 != null)
            {
                Console.WriteLine("{0}--{1}-->{2}", state.Id, state.Char, state.Output1.Id);
                OutputNFA(state.Output1);
            }
        }

        private struct NFALine
        {
            public int NodePrev;
            public int NodeNext;
            public char Symbol;
        }

        private const char Empty = '*';
        private const char BracketContent = '#';
        private const char Bottom = '$';

        private static void RegexToNFA(string regex)
        {
            int statusId = 0;
            List<KeyValuePair<char, KeyValuePair<int, int>>> NFA = new List<KeyValuePair<char, KeyValuePair<int, int>>>();
            Stack<int> startNodeStack = new Stack<int>();
            Stack<int> endNodeStack = new Stack<int>();
            Stack<char> symbol = new Stack<char>();

            int start, end;
            KeyValuePair<int, int> startend;
            char currentChar;

            startNodeStack.Push(statusId++);
            symbol.Push(Bottom);

            for (int i = 0; i < regex.Length; i++)
            {
                currentChar = regex[i];
                switch (currentChar)
                {
                    case '(':
                        start = startNodeStack.Peek();
                        startNodeStack.Push(statusId++);
                        end = startNodeStack.Peek();
                        startend = new KeyValuePair<int, int>(start, end);
                        NFA.Add(new KeyValuePair<char, KeyValuePair<int, int>>(Empty, startend)); // 插入空符号
                        symbol.Push(currentChar);
                        break;
                    case ')':
                        int last = startNodeStack.Peek();
                        PopUntil(NFA, startNodeStack, symbol, '(');
                        endNodeStack.Push(statusId++);
                        startend = new KeyValuePair<int, int>(last, endNodeStack.Peek());
                        NFA.Add(new KeyValuePair<char, KeyValuePair<int, int>>(Empty, startend));
                        symbol.Pop();
                        symbol.Push(BracketContent);
                        startNodeStack.Push(endNodeStack.Pop());
                        break;
                    case '[':
                        startNodeStack.Push(statusId++);
                        symbol.Push(currentChar);
                        break;
                    case ']':
                        break;
                    case '-':
                        break;
                    default:
                        startNodeStack.Push(statusId++);
                        symbol.Push(regex[i]);
                        break;
                }
            }
            PopUntil(NFA, startNodeStack, symbol, Bottom);
            PrintFA(NFA);
        }

        private static char PopUntil(List<KeyValuePair<char, KeyValuePair<int, int>>> NFA, Stack<int> startNodeStack, Stack<char> symbol, char until)
        {
            char currentChar = symbol.Peek();
            while (symbol.Peek() != until)
            {
                var end = startNodeStack.Pop();
                var start = startNodeStack.Peek();
                if (currentChar == BracketContent)
                {
                    startNodeStack.Pop();
                }
                else
                {
                    var startend = new KeyValuePair<int, int>(start, end);
                    NFA.Add(new KeyValuePair<char, KeyValuePair<int, int>>(currentChar, startend));
                }
                symbol.Pop();
                currentChar = symbol.Peek();
            }

            return currentChar;
        }

        private static void PrintFA(List<KeyValuePair<char, KeyValuePair<int, int>>> NFA)
        {
            foreach (var pair in NFA)
                Console.WriteLine(string.Format("{0}--{1}-->{2}", pair.Value.Key, pair.Key, pair.Value.Value));
        }
    }
}

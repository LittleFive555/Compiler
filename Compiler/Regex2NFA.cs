namespace Compiler
{
    internal class Regex2NFA
    {
        struct Paren
        {
            /// <summary>
            /// 用来标识使用 | 连接的数量
            /// </summary>
            public int nalt;
            /// <summary>
            /// 用来标识是否添加显示连接符（大于0表示当前有一个左字符）
            /// </summary>
            public int natom;
        }

        /// <summary>
        /// 显示连接符
        /// </summary>
        private const char ConcatenationOperator = '.';
        private const int MaxParenLayer = 100;

        private enum Result
        {
            Match = 256,
            Split = 257
        };

        private static int StateId;

        private struct Fragment
        {
            public int StartState;
            public int EndState;
        }

        public static FA? Execute(string regex)
        {
            var post = Regex2Post(regex);
            return Post2NFA(post);
        }

        private static string? Regex2Post(string regex)
        {
            int currentIndex = 0;
            // nalt用来标识使用 | 连接的数量
            // natom用来标识是否添加显示连接符（大于0表示当前有一个左字符）
            int nalt, natom;
            char[] buffer = new char[8000];
            int bufferIndex = 0;
            Paren[] paren = new Paren[MaxParenLayer];
            int parenLayer = 0;
            nalt = 0;
            natom = 0;

            if (regex.Length >= buffer.Length / 2)
            {
                return null;
            }

            for (; currentIndex < regex.Length; currentIndex++)
            {
                var currentChar = regex[currentIndex];
                switch (currentChar)
                {
                    case '(':
                        if (natom > 1)
                        {
                            --natom;
                            buffer[bufferIndex++] = ConcatenationOperator;
                        }
                        if (parenLayer >= MaxParenLayer) // 表示超出圆括号嵌套层数
                            return null;
                        paren[parenLayer].nalt = nalt;
                        paren[parenLayer].natom = natom;
                        parenLayer++;
                        nalt = 0;
                        natom = 0;
                        break;
                    case '|':
                        if (natom == 0) // 表示 | 符号左侧没有字符
                            return null;
                        while (--natom > 0)
                            buffer[bufferIndex++] = ConcatenationOperator;
                        nalt++;
                        break;
                    case ')':
                        if (parenLayer == 0)
                            return null;
                        if (natom == 0)
                            return null;
                        while (--natom > 0)
                            buffer[bufferIndex++] = ConcatenationOperator;
                        for (; nalt > 0; nalt--)
                            buffer[bufferIndex++] = '|';
                        --parenLayer;
                        nalt = paren[parenLayer].nalt;
                        natom = paren[parenLayer].natom;
                        natom++;
                        break;
                    case '*':
                    case '+':
                    case '?':
                        if (natom == 0)
                            return null;
                        buffer[bufferIndex++] = currentChar;
                        break;
                    default:
                        if (natom > 1)
                        {
                            natom--;
                            buffer[bufferIndex++] = ConcatenationOperator;
                        }
                        buffer[bufferIndex++] = currentChar;
                        natom++;
                        break;
                }
            }
            if (parenLayer != 0)
                return null;
            while (--natom > 0)
                buffer[bufferIndex++] = ConcatenationOperator;
            for (; nalt > 0; nalt--)
                buffer[bufferIndex++] = '|';
            return new string(buffer);
        }

        private static FA? Post2NFA(string? postfix)
        {
            if (string.IsNullOrEmpty(postfix))
                return null;

            FA nfa = new FA();
            Stack<Fragment> fragmentStack = new Stack<Fragment>();
            Fragment frag;
            Fragment fragment1, fragment2;

            int start;
            int end;
            Line line;
            for (int i = 0; i < postfix.Length && postfix[i] != '\0'; i++)
            {
                char c = postfix[i];
                switch (c)
                {
                    case '.':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment2.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment2.EndState};
                        fragmentStack.Push(frag);
                        break;
                    case '|':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();

                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        end = fragment2.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment();
                        frag.StartState = start;

                        start = fragment1.EndState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        start = fragment2.EndState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag.EndState = end;

                        fragmentStack.Push(frag);
                        break;
                    case '?':
                        frag = new Fragment();
                        fragment1 = fragmentStack.Pop();
                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState= end };
                        nfa.AddLine(line);
                        frag.StartState = start;

                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag.EndState = end;

                        start = fragment1.EndState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        fragmentStack.Push(frag);
                        break;
                    case '*':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        start = fragment1.StartState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = start, EndState = end };
                        fragmentStack.Push(frag);
                        break;
                    case '+':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = FAHelpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment1.EndState };
                        fragmentStack.Push(frag);
                        break;
                    default:
                        start = StateId++;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = c, EndState = end };
                        nfa.AddLine(line);
                        fragmentStack.Push(new Fragment() {StartState = start, EndState = end});
                        break;
                }
            }
            frag = fragmentStack.Pop();
            if (fragmentStack.Count != 0)
                return null;

            HashSet<int> receiveStates = new HashSet<int>() { frag.EndState };
            CollectReceiveStatus(nfa, frag.EndState, receiveStates);
            nfa.SetStartAndReceive(frag.StartState, receiveStates);
            return nfa;
        }

        private static void CollectReceiveStatus(FA nfa, int endState, ICollection<int> result)
        {
            if (!nfa.LinesByEndState.ContainsKey(endState))
                return;

            foreach (var line in nfa.LinesByEndState[endState])
            {
                int startState = line.StartState;
                if (line.Symbol == FAHelpers.EmptyOperator && !result.Contains(startState))
                {
                    result.Add(startState);
                    CollectReceiveStatus(nfa, startState, result);
                }
            }
        }
    }
}

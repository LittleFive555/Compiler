using System.Collections.Generic;

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
        private const char EmptyOperator = ' ';
        private const int MaxParenLayer = 100;

        public static string? Regex2Post(string regex)
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

        enum Result
        {
            Match = 256,
            Split = 257
        };

        public static int StateId;

        public struct Line
        {
            public char Symbol;
            public int StartState;
            public int EndState;

            public override string ToString()
            {
                return $"{StartState}--{Symbol}-->{EndState}";
            }
        }

        public struct Fragment
        {
            public int StartState;
            public int EndState;
        }

        public struct NFA
        {
            public int StartState;
            public int ReceiveState;
            public Dictionary<int, List<Line>> Lines;
        }

        public static void AddLine(Dictionary<int, List<Line>> nfa, Line line)
        {
            if (nfa.ContainsKey(line.StartState))
                nfa[line.StartState].Add(line);
            else
                nfa[line.StartState] = new List<Line>() { line };
        }

        public static NFA? Post2NFA(string postfix)
        {
            if (string.IsNullOrEmpty(postfix))
                return null;

            Dictionary<int, List<Line>> lines = new Dictionary<int, List<Line>>();
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
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment2.EndState};
                        fragmentStack.Push(frag);
                        break;
                    case '|':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();

                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        end = fragment2.EndState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag = new Fragment();
                        frag.StartState = start;

                        start = fragment1.EndState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        start = fragment2.EndState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag.EndState = end;

                        fragmentStack.Push(frag);
                        break;
                    case '?':
                        frag = new Fragment();
                        fragment1 = fragmentStack.Pop();
                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState= end };
                        AddLine(lines, line);
                        frag.StartState = start;

                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag.EndState = end;

                        start = fragment1.EndState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        fragmentStack.Push(frag);
                        break;
                    case '*':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        start = fragment1.StartState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag = new Fragment() { StartState = start, EndState = end };
                        fragmentStack.Push(frag);
                        break;
                    case '+':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = EmptyOperator, EndState = end };
                        AddLine(lines, line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment1.EndState };
                        fragmentStack.Push(frag);
                        break;
                    default:
                        start = StateId++;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = c, EndState = end };
                        AddLine(lines, line);
                        fragmentStack.Push(new Fragment() {StartState = start, EndState = end});
                        break;
                }
            }
            frag = fragmentStack.Pop();
            if (fragmentStack.Count != 0)
                return null;
            return new NFA() { StartState = frag.StartState, ReceiveState = frag.EndState, Lines = lines };
        }
    }
}

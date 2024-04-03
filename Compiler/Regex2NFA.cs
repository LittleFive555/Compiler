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

        public class State
        {
            public int Id;
            public int Char;
            public State Output;
            public State Output1;
            public int LastList;

            public State(int c, State output, State output1)
            {
                Id = StateId++;

                Char = c;
                Output = output;
                Output1 = output1;
                LastList = 0;
            }
        }

        static State matchState = new State((int)Result.Match, null, null);

        class Fragment
        {
            public State In;
            public List<State> Out;

            public Fragment(State start)
            {
                In = start;
                Out = new List<State>();
            }

            public void Patch(State state)
            {
                foreach (var outstate in Out)
                    outstate.Output = state;
                Out.Add(state);
            }

            public void Append(Fragment fragment)
            {
                Out.AddRange(fragment.Out);
            }
        }

        public static State Post2NFA(string postfix)
        {
            if (string.IsNullOrEmpty(postfix))
                return null;

            Stack<Fragment> fragmentStack = new Stack<Fragment>();
            Fragment frag;
            Fragment fragment1, fragment2;
            State state;

            for (int i = 0; i < postfix.Length; i++)
            {
                char c = postfix[i];
                switch (c)
                {
                    case '.':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();
                        fragment1.Patch(fragment2.In);
                        frag = new Fragment(fragment1.In);
                        fragmentStack.Push(frag);
                        break;
                    case '|':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();
                        state = new State((int)Result.Split, fragment1.In, fragment2.In);
                        frag = new Fragment(state);
                        frag.Append(fragment1);
                        frag.Append(fragment2);
                        fragmentStack.Push(frag);
                        break;
                    case '?':
                        fragment1 = fragmentStack.Pop();
                        state = new State((int)Result.Split, fragment1.In, null);
                        fragment1.Patch(state.Output1);
                        frag = new Fragment(state);
                        frag.Append(fragment1);
                        fragmentStack.Push(frag);
                        break;
                    case '*':
                        fragment1 = fragmentStack.Pop();
                        state = new State((int)Result.Split, fragment1.In, null);
                        fragment1.Patch(state);
                        frag = new Fragment(state);
                        frag.Patch(state.Output1);
                        fragmentStack.Push(frag);
                        break;
                    case '+':
                        fragment1 = fragmentStack.Pop();
                        state = new State((int)Result.Split, fragment1.In, null);
                        fragment1.Patch(state);
                        frag = new Fragment(fragment1.In);
                        frag.Patch(state.Output1);
                        fragmentStack.Push(frag);
                        break;
                    default:
                        fragmentStack.Push(new Fragment(new State(c, null, null)));
                        break;
                }
            }
            frag = fragmentStack.Pop();
            if (fragmentStack.Count != 0)
                return null;

            frag.Patch(matchState);
            return frag.In;
        }
    }
}

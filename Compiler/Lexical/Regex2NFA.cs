using System.Text;

namespace Compiler.Lexical
{
    internal class LexicalRegex
    {
        public string Name { get; }
        public LexicalType Type { get; }
        public int Priority { get; }

        public string RegexContent { get; private set; }

        public LexicalRegex(string name, string regexContent, LexicalType type, int priority)
        {
            Name = name;
            RegexContent = regexContent;
            Type = type;
            Priority = priority;
        }

        public void ChangeRegex(string regexContent)
        {
            RegexContent = regexContent;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Regex: {1}, Priority: {2}", Name, RegexContent, Priority);
        }
    }

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
        private const char ConcatenationOperator = '—';
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

        public static FA? Execute(LexicalRegex lexicalRegex)
        {
            var post = Regex2Post(lexicalRegex.RegexContent);
            return Post2NFA(post, new LexicalUnit(lexicalRegex.Name, lexicalRegex.Type, lexicalRegex.Priority));
        }

        public static FA? Execute(params LexicalRegex[] lexicalRegexs)
        {
            ReplaceRegex(lexicalRegexs);
            //foreach (var regex in lexicalRegexs)
            //    Console.WriteLine(regex.ToString());

            List<FA> nfaList = new List<FA>();
            foreach (var lexicalRegex in lexicalRegexs)
            {
                var post = Regex2Post(lexicalRegex.RegexContent);
                var nfa = Post2NFA(post, new LexicalUnit(lexicalRegex.Name, lexicalRegex.Type, lexicalRegex.Priority));
                nfaList.Add(nfa);
            }

            FA mergedNfa = new FA();
            int start = StateId++;
            Dictionary<int, SortedList<int, LexicalUnit>> receiveStates = new Dictionary<int, SortedList<int, LexicalUnit>>();
            foreach (var nfa in nfaList)
            {
                foreach (var lineByStartState in nfa.LinesByStartState)
                    mergedNfa.LinesByStartState.Add(lineByStartState.Key, lineByStartState.Value);
                foreach (var lineByEndState in nfa.LinesByEndState)
                    mergedNfa.LinesByEndState.Add(lineByEndState.Key, lineByEndState.Value);
                foreach (var symbol in nfa.AllChars)
                {
                    if (!mergedNfa.AllChars.Contains(symbol))
                        mergedNfa.AllChars.Add(symbol);
                }
                foreach (var receiveState in nfa.ReceiveStates)
                    receiveStates.Add(receiveState.Key, receiveState.Value);
                mergedNfa.AddLine(new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = nfa.StartState });
            }
            mergedNfa.SetStartAndReceive(start, receiveStates);
            return mergedNfa;
        }

        /// <summary>
        /// 替换递归定义的正则表达式
        /// </summary>
        /// <param name="lexicalRegexs"></param>
        /// <exception cref="Exception"></exception>
        private static void ReplaceRegex(LexicalRegex[] lexicalRegexs)
        {
            for (int i = 0; i < lexicalRegexs.Length; i++)
            {
                string currentRegex = lexicalRegexs[i].RegexContent;
                int subregexNameStartIndex = 0;
                bool isReadingSubregexName = false;
                StringBuilder subregexNameBuilder = new StringBuilder();
                for (int j = 0; j < currentRegex.Length; j++)
                {
                    char? lastChar = j == 0 ? null : currentRegex[j - 1];
                    char c = currentRegex[j];
                    if (c == '`' && (lastChar == null || lastChar != '\\'))
                    {
                        subregexNameBuilder.Append(c);
                        if (!isReadingSubregexName)
                        {
                            isReadingSubregexName = true;
                            subregexNameStartIndex = j;
                        }
                        else // 找到了完整的正则定义名
                        {
                            isReadingSubregexName = false;
                            var regexName = subregexNameBuilder.ToString();
                            subregexNameBuilder.Clear();
                            string regexToReplace = null;
                            for (int k = 0; k < i; k++)
                            {
                                if (lexicalRegexs[k].Name == regexName.Trim('`'))
                                {
                                    regexToReplace = lexicalRegexs[k].RegexContent;
                                    break;
                                }
                            }
                            if (regexToReplace == null)
                            {
                                throw new Exception(); // TODO 提示没有找到递归定义的正则表达式
                            }
                            else
                            {
                                regexToReplace = string.Format("({0})", regexToReplace);
                                StringBuilder newRegexBuilder = new StringBuilder(currentRegex);
                                newRegexBuilder.Replace(regexName, regexToReplace);
                                currentRegex = newRegexBuilder.ToString();
                                lexicalRegexs[i].ChangeRegex(currentRegex);
                                j = subregexNameStartIndex + regexToReplace.Length;
                            }
                        }
                    }
                    else
                    {
                        if (isReadingSubregexName)
                            subregexNameBuilder.Append(c);
                    }
                }
            }
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
                    case '\\': // NOTE 如果有转义符，则多向前读一个字符，并直接按default走一次
                        var nextChar = regex[++currentIndex];
                        if (natom > 1)
                        {
                            natom--;
                            buffer[bufferIndex++] = ConcatenationOperator;
                        }
                        buffer[bufferIndex++] = currentChar;
                        buffer[bufferIndex++] = nextChar;
                        natom++;
                        break;
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

        private static FA? Post2NFA(string? postfix, LexicalUnit lexicalUnit)
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
                    case '\\': // NOTE 如果有转义符，则多向前读一个字符，并直接按default走一次
                        var nextChar = postfix[++i];
                        start = StateId++;
                        end = StateId++;
                        line = new Line { StartState = start, Symbol = nextChar, EndState = end };
                        nfa.AddLine(line);
                        fragmentStack.Push(new Fragment() { StartState = start, EndState = end });
                        break;
                    case ConcatenationOperator:
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment2.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment2.EndState };
                        fragmentStack.Push(frag);
                        break;
                    case '|':
                        fragment2 = fragmentStack.Pop();
                        fragment1 = fragmentStack.Pop();

                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        end = fragment2.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment();
                        frag.StartState = start;

                        start = fragment1.EndState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        start = fragment2.EndState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag.EndState = end;

                        fragmentStack.Push(frag);
                        break;
                    case '?':
                        frag = new Fragment();
                        fragment1 = fragmentStack.Pop();
                        start = StateId++;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag.StartState = start;

                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag.EndState = end;

                        start = fragment1.EndState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        fragmentStack.Push(frag);
                        break;
                    case '*':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        start = fragment1.StartState;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = start, EndState = end };
                        fragmentStack.Push(frag);
                        break;
                    case '+':
                        fragment1 = fragmentStack.Pop();
                        start = fragment1.EndState;
                        end = fragment1.StartState;
                        line = new Line() { StartState = start, Symbol = Helpers.EmptyOperator, EndState = end };
                        nfa.AddLine(line);
                        frag = new Fragment() { StartState = fragment1.StartState, EndState = fragment1.EndState };
                        fragmentStack.Push(frag);
                        break;
                    default:
                        start = StateId++;
                        end = StateId++;
                        line = new Line() { StartState = start, Symbol = c, EndState = end };
                        nfa.AddLine(line);
                        fragmentStack.Push(new Fragment() { StartState = start, EndState = end });
                        break;
                }
            }
            frag = fragmentStack.Pop();
            if (fragmentStack.Count != 0)
                return null;

            Dictionary<int, SortedList<int, LexicalUnit>> receiveStates = new Dictionary<int, SortedList<int, LexicalUnit>>()
            {
                { frag.EndState, new SortedList<int, LexicalUnit>(){ { lexicalUnit.Priority, lexicalUnit } } }
            };
            CollectReceiveStatus(nfa, frag.EndState, receiveStates, lexicalUnit);
            nfa.SetStartAndReceive(frag.StartState, receiveStates);
            return nfa;
        }

        private static void CollectReceiveStatus(FA nfa, int endState, Dictionary<int, SortedList<int, LexicalUnit>> result, LexicalUnit lexicalUnit)
        {
            if (!nfa.LinesByEndState.ContainsKey(endState))
                return;

            foreach (var line in nfa.LinesByEndState[endState])
            {
                int startState = line.StartState;
                if (line.Symbol == Helpers.EmptyOperator && !result.Keys.Contains(startState))
                {
                    result.Add(startState, new SortedList<int, LexicalUnit>() { { lexicalUnit.Priority, lexicalUnit } });
                    CollectReceiveStatus(nfa, startState, result, lexicalUnit);
                }
            }
        }
    }
}

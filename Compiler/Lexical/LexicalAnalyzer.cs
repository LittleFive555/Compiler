﻿using System.Text;

namespace Compiler.Lexical
{
    internal class LexicalAnalyzer
    {
        private FA m_nfa;
        private FA m_dfa;

        public LexicalAnalyzer(params LexicalRegex[] lexicalRegexs)
        {
            m_nfa = Regex2NFA.Execute(lexicalRegexs);
            //Console.WriteLine("NFA:");
            //Console.WriteLine(nfa);
            m_dfa = NFA2DFA.Execute(m_nfa);
            //Console.WriteLine("DFA:");
            //Console.WriteLine(dfa);
        }

        public List<Token> Read(Stream stream)
        {
            List<Token> result = new List<Token>();

            long lexemeBegin = stream.Position;
            long lastReceivePos = lexemeBegin;
            int lineForward = 1;
            int lineOnLastReceive = lineForward;
            long positionOnNewLine = lexemeBegin - 1;
            string lastReceiveStr = null;
            StringBuilder stringBuilder = new StringBuilder();
            int currentDFAStateId = m_dfa.StartState;
            LexicalUnit? lexicalUnit = null;
            while (true)
            {
                char c = ReadChar(stream);
                long forward = stream.Position;
                stringBuilder.Append(c);

                if (c == '\n')
                {
                    lineForward++;
                    positionOnNewLine = forward - 1;
                }

                if (CanMove(m_dfa, currentDFAStateId, c, out int nextStateId))
                {
                    currentDFAStateId = nextStateId;
                    if (m_dfa.IsReceiveState(currentDFAStateId, out SortedList<int, LexicalUnit> lexicalUnits))
                    {
                        if (c == '"' || c == '\'' || c == '`') // 对字符串的处理
                        {
                            char strStart = c;
                            do
                            {
                                c = ReadChar(stream);
                                forward = stream.Position;
                                stringBuilder.Append(c);

                            } while (c != strStart && !c.Equals('\0'));

                            if (c == strStart)
                            {
                                lexicalUnit = new LexicalUnit() { Name = "String", Priority = -1 };
                                lastReceivePos = forward;
                                lineOnLastReceive = lineForward;
                                lastReceiveStr = stringBuilder.ToString();
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        else
                        {
                            lexicalUnit = lexicalUnits.Values[lexicalUnits.Count - 1];
                            lastReceivePos = forward;
                            lineOnLastReceive = lineForward;
                            lastReceiveStr = stringBuilder.ToString();
                        }
                    }
                }
                else
                {
                    if (lexicalUnit != null)
                    {
                        if (lexicalUnit.Name != Helpers.WhitespaceName) // 跳过空白
                        {
                            Token token = new Token(lastReceiveStr,
                                lexicalUnit,
                                lineForward,
                                (int)(lexemeBegin - positionOnNewLine),
                                (int)(lastReceivePos - lexemeBegin));
                            result.Add(token);
                        }

                        currentDFAStateId = m_dfa.StartState;
                        stream.Seek(lastReceivePos, SeekOrigin.Begin);
                        lineForward = lineOnLastReceive;
                        stringBuilder.Clear();
                        lexemeBegin = stream.Position;
                        lexicalUnit = null;
                    }
                    else
                    {
                        // TODO 尝试错误恢复，将错误保存起来
                        throw new Exception();
                    }
                }
                if (c.Equals('\0'))
                    break;
            }
            return result;
        }

        private static bool CanMove(FA dfa, int currentStateId, char input, out int nextStateId)
        {
            if (dfa.LinesByStartState.TryGetValue(currentStateId, out var lines))
            {
                foreach (var line in lines)
                {
                    if (line.Symbol.Equals(input))
                    {
                        nextStateId = line.EndState;
                        return true;
                    }
                }
            }
            nextStateId = -1;
            return false;
        }

        private static char ReadChar(Stream stream)
        {
            int readIn = stream.ReadByte();
            if (readIn == -1)
                return '\0';
            else
                return (char)readIn;
        }
    }
}

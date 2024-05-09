using System.Text;

namespace Compiler.Lexical
{
    internal class LexicalAnalyzer
    {
        private FA m_nfa;
        private FA m_dfa;

        public LexicalAnalyzer(params LexicalRegex[] lexicalRegexs)
        {
            m_nfa = Regex2NFA.Execute(lexicalRegexs);
            m_dfa = NFA2DFA.Execute(m_nfa);
        }

        public Result Read(FileData fileData, Stream stream)
        {
            Result result = new Result();

            long lexemeBegin = stream.Position;
            long lastReceivePos = lexemeBegin;
            int lineForward = 1;
            int lineOnLastReceive = lineForward;
            long positionOnNewLine = lexemeBegin - 1;
            string lastReceiveStr = null;
            StringBuilder stringBuilder = new StringBuilder();
            int currentDFAStateId = m_dfa.StartState;
            LexicalUnit? lexicalUnit = null;

            bool isSingleLineComment = false;
            bool isBlockComment = false;

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

                if (isSingleLineComment)
                {
                    if (c == '\n' || c == '\0')
                    {
                        isSingleLineComment = false;

                        Token token = new Token(stringBuilder.ToString(),
                            new LexicalUnit(Helpers.SingleLineCommentName, LexicalType.Comment, 1000),
                            fileData,
                            lineForward,
                            (int)(lexemeBegin - positionOnNewLine),
                            (int)(forward - lexemeBegin));
                        result.AppendToken(token);

                        currentDFAStateId = m_dfa.StartState;
                        stringBuilder.Clear();
                        lexemeBegin = stream.Position;
                        lexicalUnit = null;
                    }
                    continue;
                }
                else if (isBlockComment)
                {
                    if ((c == '*' && PeekChar(stream) == '/') || c == '\0')
                    {
                        isBlockComment = false;

                        c = ReadChar(stream);
                        forward = stream.Position;
                        stringBuilder.Append(c);

                        Token token = new Token(stringBuilder.ToString(),
                            new LexicalUnit(Helpers.BlockCommentName, LexicalType.Comment, 1001),
                            fileData,
                            lineForward,
                            (int)(lexemeBegin - positionOnNewLine),
                            (int)(forward - lexemeBegin));
                        result.AppendToken(token);

                        currentDFAStateId = m_dfa.StartState;
                        stringBuilder.Clear();
                        lexemeBegin = stream.Position;
                        lexicalUnit = null;
                    }
                    continue;
                }

                if (CanMove(m_dfa, currentDFAStateId, c, out int nextStateId))
                {
                    currentDFAStateId = nextStateId;
                    if (m_dfa.IsReceiveState(currentDFAStateId, out SortedList<int, LexicalUnit> lexicalUnits))
                    {
                        if (c == '"' || c == '\'' || c == '`') // 对字符串的处理
                        {
                            char strStart = c;
                            char lastChar;
                            do
                            {
                                lastChar = c;
                                c = ReadChar(stream);
                                forward = stream.Position;
                                stringBuilder.Append(c);
                            } while (((lastChar == '\\' && c == strStart) || c != strStart) && c != '\0');

                            if (c == strStart)
                            {
                                lexicalUnit = new LexicalUnit("String", LexicalType.Other, -1);
                                lastReceivePos = forward;
                                lineOnLastReceive = lineForward;
                                lastReceiveStr = stringBuilder.ToString();
                            }
                            else
                            {
                                result.AppendError(new CompileError(lineForward,
                                    (int)(lexemeBegin - positionOnNewLine),
                                    (int)(forward - lexemeBegin),
                                    string.Format("发现未配对的字符串符号 {0}", strStart)));
                                // TODO 进行错误恢复
                            }
                        }
                        else if (lexicalUnits.Values[lexicalUnits.Count - 1].Name == Helpers.SingleLineCommentName)
                            isSingleLineComment = true;
                        else if (lexicalUnits.Values[lexicalUnits.Count - 1].Name == Helpers.BlockCommentLeftName)
                            isBlockComment = true;
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
                        if (lexicalUnit.LexicalType != LexicalType.Whitespace) // 跳过空白
                        {
                            Token token = new Token(lastReceiveStr,
                                lexicalUnit,
                                fileData,
                                lineForward,
                                (int)(lexemeBegin - positionOnNewLine),
                                (int)(lastReceivePos - lexemeBegin));
                            result.AppendToken(token);
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
                        result.AppendError(new CompileError(lineForward,
                            (int)(lexemeBegin - positionOnNewLine),
                            (int)(forward - lexemeBegin),
                            "非法的词法单元"));
                        // TODO 尝试错误恢复
                    }
                }
                if (c == '\0')
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
                    if (line.Symbol == input)
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

        private static char PeekChar(Stream stream)
        {
            long lastPosition = stream.Position;
            char c = ReadChar(stream);
            stream.Position = lastPosition;
            return c;
        }

        internal class Result
        {
            public List<Token> Tokens = new List<Token>();
            public List<CompileError> Errors = new List<CompileError>();

            public void AppendToken(Token token)
            {
                Tokens.Add(token);
            }

            public void AppendError(CompileError error)
            {
                Errors.Add(error);
            }
        }
    }
}

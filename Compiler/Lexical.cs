using System.Text;

namespace Compiler
{
    public class Lexical
    {
        public static List<Token> Read(Stream stream, FA dfa)
        {
            List<Token> result = new List<Token>();
            long lexemeBegin = stream.Position;
            long lastReceivePos = lexemeBegin;
            string lastReceiveStr = null;
            StringBuilder stringBuilder = new StringBuilder();
            int currentDFAStateId = dfa.StartState;
            LexicalUnit? lexicalUnit = null;
            while (true)
            {
                char c = ReadChar(stream);
                long forward = stream.Position;
                stringBuilder.Append(c);
                if (CanMove(dfa, currentDFAStateId, c, out int nextStateId))
                {
                    currentDFAStateId = nextStateId;
                    if (dfa.IsReceiveState(currentDFAStateId, out SortedList<int, LexicalUnit> lexicalUnits))
                    {
                        lexicalUnit = lexicalUnits.Values[lexicalUnits.Count - 1];
                        lastReceivePos = forward;
                        lastReceiveStr = stringBuilder.ToString();
                    }
                }
                else
                {
                    if (lexicalUnit != null)
                    {
                        if (lexicalUnit.Name != Helpers.WhitespaceName) // 跳过空白
                        {
                            Token token = new Token()
                            {
                                Content = lastReceiveStr,
                                LexicalUnit = lexicalUnit,
                                Length = (int)(lastReceivePos - lexemeBegin),
                            };
                            result.Add(token);
                        }

                        currentDFAStateId = dfa.StartState;
                        stream.Seek(lastReceivePos, SeekOrigin.Begin);
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

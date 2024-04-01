namespace Compiler
{
    internal class Regex2NFA
    {
        struct Paren
        {
            public int nalt;
            public int natom;
        }

        private const char ConcatenationOperator = '.';

        public static string? Regex2Post(string regex)
        {
            int currentIndex = 0;
            int nalt, natom;
            char[] buffer = new char[8000];
            int bufferIndex = 0;
            Paren[] paren = new Paren[100];
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
                        if (parenLayer >= paren.Length)
                            return null;
                        paren[parenLayer].nalt = nalt;
                        paren[parenLayer].natom = natom;
                        parenLayer++;
                        nalt = 0;
                        natom = 0;
                        break;
                    case '|':
                        if (natom == 0)
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

        int nstate;

        class State
        {
            public int Char;
            public State Output;
            public State Output1;
            public int LastList;

            public State(int c, State output, State output1)
            {
                Char = c;
                Output = output;
                Output1 = output1;
                LastList = 0;
            }
        }

        State matchState = new State((int)Result.Match, null, null);
    }
}

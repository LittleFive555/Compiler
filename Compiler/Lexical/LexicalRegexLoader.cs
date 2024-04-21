namespace Compiler.Lexical
{
    public enum LexicalType
    {
        Symbol,
        ReservedWord,
        Whitespace,
        Comment,
        Other,
    }

    internal static class LexicalRegexLoader
    {
        private static int Priority = 0;

        public static List<LexicalRegex> ReadRegexFromFile(LexicalType lexicalType, string filePath)
        {
            List<LexicalRegex> allLexicalRegex = new List<LexicalRegex>();
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                while (true)
                {
                    var lineContent = streamReader.ReadLine();
                    if (lineContent == null) // 表示读到末尾
                        break;

                    if (string.IsNullOrEmpty(lineContent)) // 表示读到空行
                        continue;

                    int seperateIndex = lineContent.IndexOf(":"); // 表示读到非正则产生式的行
                    if (seperateIndex == -1)
                        continue;

                    lineContent = lineContent.TrimEnd('\r', '\n');
                    string name = lineContent.Substring(0, seperateIndex);
                    string regex = lineContent.Substring(seperateIndex + 1);
                    int currentPriority = Priority++;

                    allLexicalRegex.Add(new LexicalRegex(name, regex, lexicalType, currentPriority));
                }
            }

            return allLexicalRegex;
        }
    }
}
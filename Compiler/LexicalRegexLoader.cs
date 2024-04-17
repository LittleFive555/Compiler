namespace Compiler
{
    internal static class LexicalRegexLoader
    {
        private static int Priority = 0;

        public static List<LexicalRegex> ReadRegexFromMultiFiles(params string[] filesPath)
        {
            List<LexicalRegex> allLexicalRegex = new List<LexicalRegex>();
            allLexicalRegex.Add(new LexicalRegex()
            {
                Name = Helpers.WhitespaceName,
                RegexContent = Helpers.WhitespaceRegex,
                Priority = Priority++
            });
            foreach (var filePath in filesPath)
                allLexicalRegex.AddRange(ReadRegexFromFile(filePath));
            return allLexicalRegex;
        }

        private static List<LexicalRegex> ReadRegexFromFile(string filePath)
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

                    allLexicalRegex.Add(new LexicalRegex()
                    {
                        Name = name,
                        RegexContent = regex,
                        Priority = currentPriority
                    });
                }
            }

            return allLexicalRegex;
        }
    }
}
using System.Text;

namespace Compiler.Syntax
{
    public class SyntaxReader
    {
        public static SyntaxLine? Read(string syntaxLine)
        {
            // TODO 可以用正则表达式来判断syntaxLine的格式是否正确

            SyntaxLine result = new SyntaxLine();
            var splited = syntaxLine.Split(':');
            if (splited.Length != 2)
                return null;

            result.Name = splited[0].Trim();
            string[] productionStrings = splited[1].Split('|');
            foreach (var productionString in productionStrings)
            {
                string trimed = productionString.Trim();
                if (string.IsNullOrEmpty(trimed))
                    continue;

                string[] symbols = trimed.Split(' ');
                Production production = new Production(result.Name);
                foreach (var symbol in symbols)
                {
                    if (string.IsNullOrEmpty(symbol))
                        continue;

                    production.Symbols.Add(symbol);
                }
                if (production.Symbols.Count > 0)
                    result.Productions.Add(production);
            }
            return result;
        }

        public static Dictionary<string, SyntaxLine> ReadFromFile(string fileName)
        {
            Dictionary<string, SyntaxLine> result = new Dictionary<string, SyntaxLine>();
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                // 先剔除注释
                while (true)
                {
                    var lineContent = streamReader.ReadLine();
                    if (lineContent == null)
                        break;

                    int index = lineContent.IndexOf("//");
                    if (index != -1)
                    {
                        string withoutComment = lineContent.Substring(0, index);
                        if (!string.IsNullOrEmpty(withoutComment))
                            stringBuilder.Append(withoutComment);
                    }
                    else
                        stringBuilder.Append(lineContent);
                }
            }

            string fileContent = stringBuilder.ToString();
            var syntaxLines = fileContent.Split("$");
            foreach (var lineContent in syntaxLines)
            {
                var syntaxLine = Read(lineContent);
                if (syntaxLine == null)
                    continue;
                result.Add(syntaxLine.Name, syntaxLine);
            }
            return result;
        }
    }
}

using System.Text;

namespace Compiler.Syntax
{
    public class SyntaxReader
    {
        public static SyntaxLine? Read(string syntaxLine)
        {
            // TODO 可以用正则表达式来判断syntaxLine的格式是否正确

            var splited = syntaxLine.Split(':');
            if (splited.Length != 2)
                return null;

            var name = splited[0].Trim();
            List<Production> productions = new List<Production>();
            string[] productionStrings = splited[1].Split('|');
            foreach (var productionString in productionStrings)
            {
                string trimed = productionString.Trim();
                if (string.IsNullOrEmpty(trimed))
                    continue;

                string[] splitedSymbols = trimed.Split(' ');
                List<string> symbols = new List<string>();
                foreach (var symbol in splitedSymbols)
                {
                    if (string.IsNullOrEmpty(symbol))
                        continue;

                    symbols.Add(symbol);
                }
                if (symbols.Count > 0)
                    productions.Add(new Production(name, symbols));
            }
            SyntaxLine result = new SyntaxLine(name, productions);
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

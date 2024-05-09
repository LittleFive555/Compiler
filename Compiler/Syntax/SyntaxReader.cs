using System.Text;
using Compiler.Syntax.ParseActions;

namespace Compiler.Syntax
{
    internal class SyntaxReader
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

                List<SyntaxUnit> symbols = new List<SyntaxUnit>();
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < trimed.Length; i++)
                {
                    char c = trimed[i];
                    if (c == '{')
                    {
                        stringBuilder.Append(c);
                        while (c != '}' && i < trimed.Length)
                        {
                            c = trimed[++i];
                            stringBuilder.Append(c);
                        }
                        if (stringBuilder[stringBuilder.Length - 1] != '}')
                            throw new Exception();

                        symbols.Add(ParseActionFactory.CreateAction(stringBuilder.ToString()));
                        stringBuilder.Clear();
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        if (stringBuilder.Length > 0)
                        {
                            symbols.Add(new SymbolName(stringBuilder.ToString()));
                            stringBuilder.Clear();
                        }
                    }
                    else
                    {
                        stringBuilder.Append(c);
                    }
                }
                if (stringBuilder.Length > 0)
                {
                    symbols.Add(new SymbolName(stringBuilder.ToString()));
                    stringBuilder.Clear();
                }

                if (symbols.Count > 0)
                    productions.Add(new Production(symbols));
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

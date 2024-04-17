using System.Text;

namespace Compiler.Syntax
{
    public class SyntaxLine
    {
        public string Name;
        public List<Production> Productions = new List<Production>();

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Name);
            stringBuilder.Append("→");

            for (int i = 0; i < Productions.Count; i++)
            {
                var production = Productions[i];
                stringBuilder.Append(production);
                if (i < Productions.Count - 1)
                {
                    stringBuilder.AppendLine();
                    for (int j = 0; j < Name.Length; j++)
                        stringBuilder.Append(' ');
                    stringBuilder.Append('|');
                }
            }
            return stringBuilder.ToString();
        }
    }

    public class Production
    {
        public string Belonged;

        public List<string> Symbols = new List<string>();

        public Production(string belonged)
        {
            Belonged = belonged;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var symbol in Symbols)
            {
                stringBuilder.Append(symbol);
                stringBuilder.Append(' ');
            }
            return stringBuilder.ToString();
        }
    }
}

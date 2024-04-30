using System.Text;

namespace Compiler.Syntax
{
    public class SyntaxLine
    {
        public string Name { get; }

        private List<Production> m_productions = new List<Production>();
        public IReadOnlyList<Production> Productions => m_productions;

        public SyntaxLine(string name, IEnumerable<Production> productions)
        {
            Name = name;
            SetProductions(productions);
        }

        public void SetProductions(IEnumerable<Production> productions)
        {
            m_productions.Clear();
            m_productions.AddRange(productions);
        }

        public static bool IsSameProductions(SyntaxLine syntaxLine1, SyntaxLine syntaxLine2)
        {
            if (syntaxLine1.Productions.Count != syntaxLine2.Productions.Count)
                return false;
            foreach (var production1 in syntaxLine1.Productions)
            {
                bool haveSameProduction = false;
                foreach (var production2 in syntaxLine2.Productions)
                {
                    if (Production.IsSameSymbolsList(production1, production2))
                    {
                        haveSameProduction = true;
                        break;
                    }
                }
                if (!haveSameProduction)
                    return false;
            }
            return true;
        }

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

    public class Production : IEquatable<Production?>
    {
        public string Belonged { get; }

        private List<string> m_symbols = new List<string>();
        public IReadOnlyList<string> Symbols => m_symbols;

        public Production(string belonged, IEnumerable<string> symbols)
        {
            Belonged = belonged;
            SetSymbolsList(symbols);
        }

        public void SetSymbolsList(IEnumerable<string> symbols)
        {
            m_symbols.Clear();
            m_symbols.AddRange(symbols);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Production);
        }

        public bool Equals(Production? other)
        {
            return other is not null &&
                   Belonged == other.Belonged &&
                   IsSameSymbolsList(this, other);
        }

        public static bool IsSameSymbolsList(Production production1, Production production2)
        {
            if (production1.Symbols.Count != production2.Symbols.Count)
                return false;

            for (int i = 0; i < production1.Symbols.Count; i++)
            {
                if (production1.Symbols[i] != production2.Symbols[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Belonged, Symbols);
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

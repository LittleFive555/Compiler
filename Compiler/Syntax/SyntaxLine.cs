using System.Text;

namespace Compiler.Syntax
{
    internal class SyntaxLine
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

    internal enum SyntaxUnitType
    {
        SymbolName,
        ParseAction,
    }

    internal class Production : IEquatable<Production?>
    {
        private List<SyntaxUnit> m_syntaxUnitList = new List<SyntaxUnit>();
        public IReadOnlyList<SyntaxUnit> SyntaxUnitList => m_syntaxUnitList;

        public Production(IEnumerable<SyntaxUnit> symbols)
        {
            SetSymbolsList(symbols);
        }

        public void SetSymbolsList(IEnumerable<SyntaxUnit> symbols)
        {
            m_syntaxUnitList.Clear();
            m_syntaxUnitList.AddRange(symbols);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Production);
        }

        public bool Equals(Production? other)
        {
            return other is not null &&
                   IsSameSymbolsList(this, other);
        }

        public static bool IsSameSymbolsList(Production production1, Production production2)
        {
            if (production1.SyntaxUnitList.Count != production2.SyntaxUnitList.Count)
                return false;

            for (int i = 0; i < production1.SyntaxUnitList.Count; i++)
            {
                if (production1.SyntaxUnitList[i] != production2.SyntaxUnitList[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SyntaxUnitList);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var symbol in SyntaxUnitList)
            {
                stringBuilder.Append(symbol);
                stringBuilder.Append(' ');
            }
            return stringBuilder.ToString();
        }
    }
}

﻿using System.Text;

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

    public class Production : IEquatable<Production?>
    {
        public string Belonged;

        public List<string> Symbols = new List<string>();

        public Production(string belonged)
        {
            Belonged = belonged;
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

        private static bool IsSameSymbolsList(Production production1, Production production2)
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

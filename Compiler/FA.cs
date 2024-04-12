using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Compiler
{
    public class LexicalUnit
    {
        public string Name;
        public int Priority;
    }

    public class FA
    {
        public int StartState;
        public Dictionary<int, SortedList<int, LexicalUnit>> ReceiveStates;

        public Dictionary<int, List<Line>> LinesByStartState;
        public Dictionary<int, List<Line>> LinesByEndState;

        public HashSet<char> AllChars;

        public FA()
        {
            LinesByStartState = new Dictionary<int, List<Line>>();
            LinesByEndState = new Dictionary<int, List<Line>>();
            AllChars = new HashSet<char>();
        }

        public void SetStartAndReceive(int start, Dictionary<int, SortedList<int, LexicalUnit>> receiveStates)
        {
            StartState = start;
            ReceiveStates = receiveStates;
        }

        public void AddLine(Line line)
        {
            if (LinesByStartState.ContainsKey(line.StartState))
                LinesByStartState[line.StartState].Add(line);
            else
                LinesByStartState[line.StartState] = new List<Line>() { line };

            if (LinesByEndState.ContainsKey(line.EndState))
                LinesByEndState[line.EndState].Add(line);
            else
                LinesByEndState[line.EndState] = new List<Line>() { line };

            if (line.Symbol != Helpers.EmptyOperator && !AllChars.Contains(line.Symbol))
                AllChars.Add(line.Symbol);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var stateLine in LinesByStartState)
            {
                foreach (var line in stateLine.Value)
                {
                    sb.Append(line.ToString());
                    if (ReceiveStates.Keys.Contains(line.EndState))
                    {
                        sb.Append(" *");
                        foreach (var lexicalUnit in ReceiveStates[line.EndState].Values)
                            sb.Append(string.Format("\n                Priority: {0} Name: {1}", lexicalUnit.Priority, lexicalUnit.Name));
                    }
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
    }

    public struct Line
    {
        public char Symbol;
        public int StartState;
        public int EndState;

        public override string ToString()
        {
            return $"{StartState}--{Symbol}-->{EndState}";
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Line line)
                return StartState == line.StartState && EndState == line.EndState && Symbol == line.Symbol;
            else 
                return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol, StartState, EndState);
        }

        public static bool operator ==(Line left, Line right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Line left, Line right)
        {
            return !(left == right);
        }
    }
}

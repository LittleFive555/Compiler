using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Compiler
{
    public class FA
    {
        public int StartState;
        public HashSet<int> ReceiveStates;

        public Dictionary<int, List<Line>> LinesByStartState;
        public Dictionary<int, List<Line>> LinesByEndState;

        public HashSet<char> AllChars;

        public FA()
        {
            LinesByStartState = new Dictionary<int, List<Line>>();
            LinesByEndState = new Dictionary<int, List<Line>>();
            AllChars = new HashSet<char>();
        }

        public void SetStartAndReceive(int start, HashSet<int> receiveStates)
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

            if (line.Symbol != FAHelpers.EmptyOperator && !AllChars.Contains(line.Symbol))
                AllChars.Add(line.Symbol);
        }

        public override string ToString()
        {
            // TODO
            StringBuilder sb = new StringBuilder();
            foreach (var stateLine in LinesByStartState)
            {
                foreach (var line in stateLine.Value)
                {
                    sb.Append(line.ToString());
                    if (ReceiveStates.Contains(line.EndState))
                        sb.Append(" *");
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

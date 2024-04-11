﻿namespace Compiler
{
    internal class NFA2DFA
    {
        private static int StateId = 0;
        public static FA? Execute(FA? nfa)
        {
            if (nfa == null)
                return null;
            FA dfa = new FA();
            Dictionary<int, bool> marks = new Dictionary<int, bool>();
            Dictionary<int, HashSet<int>> dfaStates = new Dictionary<int, HashSet<int>>();
            int dfaStartState = StateId++;

            dfaStates.Add(dfaStartState, new HashSet<int>() { nfa.StartState });
            marks.Add(dfaStartState, false);
            CollectEmptyClosure(nfa, nfa.StartState, dfaStates[dfaStartState]);
            HashSet<int> dfaReceiveStates = new HashSet<int>();
            while (TryGetUnmarked(marks, out int stateId))
            {
                marks[stateId] = true;
                foreach (var inputChar in nfa.AllChars) // 对于所有输入字符
                {
                    HashSet<int> newStateSet = new HashSet<int>();
                    foreach (var nfaState in dfaStates[stateId]) // 对于当前DFA状态包含的所有NFA状态
                    {
                        if (!nfa.LinesByStartState.ContainsKey(nfaState))
                            continue;

                        foreach (var line in nfa.LinesByStartState[nfaState])
                        {
                            if (line.Symbol == inputChar && !newStateSet.Contains(line.EndState)) // 经过该字符所能到达的状态
                            {
                                newStateSet.Add(line.EndState);
                                CollectEmptyClosure(nfa, line.EndState, newStateSet);
                            }
                        }
                    }

                    if (newStateSet.Count == 0)
                        continue;

                    bool isAdded = false;
                    int dfaStateId = -1;
                    foreach (var dfaState in dfaStates)
                    {
                        if (IsSame(dfaState.Value, newStateSet))
                        {
                            isAdded = true;
                            dfaStateId = dfaState.Key;
                            break;
                        }
                    }
                    if (isAdded)
                        continue;

                    dfaStateId = StateId++;
                    dfaStates.Add(dfaStateId, newStateSet);
                    marks.Add(dfaStateId, false);
                    foreach (var nfaState in newStateSet)
                    {
                        if (nfa.ReceiveStates.Contains(nfaState))
                        {
                            dfaReceiveStates.Add(dfaStateId);
                            break;
                        }
                    }
                    dfa.AddLine(new Line() { StartState = stateId, Symbol = inputChar, EndState = dfaStateId });
                }
            }
            dfa.SetStartAndReceive(dfaStartState, dfaReceiveStates);
            return dfa;
        }

        private static void CollectEmptyClosure(FA nfa, int startState, ICollection<int> result)
        {
            if (!nfa.LinesByStartState.ContainsKey(startState))
                return;

            foreach (var line in nfa.LinesByStartState[startState])
            {
                int endState = line.EndState;
                if (line.Symbol == FAHelpers.EmptyOperator && !result.Contains(endState))
                {
                    result.Add(endState);
                    CollectEmptyClosure(nfa, endState, result);
                }
            }
        }

        private static bool IsSame(HashSet<int> set1, HashSet<int> set2)
        {
            if (set1.Count != set2.Count)
                return false;
            foreach (var value in set1)
            {
                if (!set2.Contains(value))
                    return false;
            }
            return true;
        }

        private static bool TryGetUnmarked(Dictionary<int, bool> marks, out int stateId)
        {
            foreach (var mark in marks)
            {
                if (!mark.Value)
                {
                    stateId = mark.Key;
                    return true;
                }
            }
            stateId = -1;
            return false;
        }
    }
}

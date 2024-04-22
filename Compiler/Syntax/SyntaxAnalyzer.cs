using System;
using System.Text;
using Compiler.Lexical;

namespace Compiler.Syntax
{
    internal class SyntaxAnalyzer
    {
        private readonly Dictionary<string, SyntaxLine> m_syntaxLines;
        private Dictionary<string, Dictionary<string, List<Production>>> m_predictiveAnylisisTable;

        private const string StartSymbol = "S";
        private const string EndSymbol = "$";

        public SyntaxAnalyzer(Dictionary<string, SyntaxLine> syntaxLines)
        {
            m_syntaxLines = syntaxLines;
            if (!m_syntaxLines.ContainsKey(StartSymbol))
                throw new Exception("没有文法开始符号 S ，请检查是否有产生式左侧命名为 S 的文法");
            Initialize();
        }

        public Result Execute(List<Token> tokens)
        {
            Result result = new Result();

            Stack<Snapshot> snapshotStack = new Stack<Snapshot>();
            Stack<string> stack = new Stack<string>();
            stack.Push(EndSymbol);
            stack.Push(StartSymbol);

            int index = 0;
            string currentSymbol = stack.Peek();
            int counter = 0;
            while (currentSymbol != EndSymbol)
            {
                #region 输出
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(counter++);
                stringBuilder.Append(" : ");
                int line = 0;
                for (int i = 0; i < index && i < tokens.Count; i++)
                {
                    if (line != tokens[i].Line)
                    {
                        stringBuilder.AppendLine();
                        line = tokens[i].Line;
                    }
                    stringBuilder.Append(tokens[i].Content);
                    stringBuilder.Append(" ");
                }
                stringBuilder.Append("\t\t\t");
                var stackList = stack.ToArray();
                for (int i = 0; i < stackList.Length; i++)
                {
                    stringBuilder.Append(stackList[i]);
                    stringBuilder.Append(" ");
                }
                MyLogger.WriteLine(stringBuilder.ToString());
                #endregion

                if (tokens[Math.Clamp(index, 0, tokens.Count - 1)].LexicalUnit.LexicalType == LexicalType.Comment)
                {
                    index++;
                    continue;
                }

                var currentToken = tokens[Math.Clamp(index, 0, tokens.Count - 1)];
                string currentTokenName = index < tokens.Count ? currentToken.LexicalUnit.Name : EndSymbol;
                if (currentSymbol.Equals(currentTokenName))
                {
                    stack.Pop();
                    if (snapshotStack.Count > 0 && stack.Count < snapshotStack.Peek().CloneStack.Count)
                        snapshotStack.Pop();
                    index++;
                }
                else if (IsTerminalSymbol(currentSymbol))
                {
                    result.AppendError(new CompileError(currentToken.Line, currentToken.StartColumn, currentToken.Length, "..."));

                    if (TryRecall(tokens, snapshotStack, out var tempStack, out var tempIndex))
                    {
                        stack = tempStack;
                        index = tempIndex;
                    }
                    else
                        throw new Exception();
                    // TODO try fix and continue
                }
                else if (!m_predictiveAnylisisTable[currentSymbol].ContainsKey(currentTokenName))
                {
                    result.AppendError(new CompileError(currentToken.Line, currentToken.StartColumn, currentToken.Length, "..."));

                    if (TryRecall(tokens, snapshotStack, out var tempStack, out var tempIndex))
                    {
                        stack = tempStack;
                        index = tempIndex;
                    }
                    else
                        throw new Exception();
                    // TODO try fix and continue
                }
                else
                {
                    var syntaxProductions = m_predictiveAnylisisTable[currentSymbol][currentTokenName];
                    if (syntaxProductions.Count > 2)
                        throw new Exception();

                    Production production;
                    if (syntaxProductions.Count == 1) // 只有一个产生式，选择该产生式
                        production = syntaxProductions[0];
                    else
                    {
                        if (IsEmptyProduction(syntaxProductions[1])) // 第一个产生式非空，第二个产生式为空，选择非空
                            production = syntaxProductions[0];
                        else // 两个产生式全非空，则准备回溯（或许可能会当有空产生式时也回溯？）
                        {
                            int productionIndex = 0;
                            var snapshot = new Snapshot(stack, index, productionIndex);
                            snapshotStack.Push(snapshot);
                            production = syntaxProductions[productionIndex];
                        }
                    }

                    if (IsEmptyProduction(production))
                    {
                        stack.Pop();
                        if (snapshotStack.Count > 0 && stack.Count < snapshotStack.Peek().CloneStack.Count)
                            snapshotStack.Pop();
                    }
                    else
                    {
                        stack.Pop();
                        for (int i = production.Symbols.Count - 1; i >= 0; i--)
                            stack.Push(production.Symbols[i]);
                    }
                }
                currentSymbol = stack.Peek();
            }
            return result;
        }

        private bool TryRecall(List<Token> tokens, Stack<Snapshot> snapshotStack, out Stack<string> stack, out int index)
        {
            while (snapshotStack.Count > 0)
            {
                var snapshot = snapshotStack.Peek();
                var currentToken = tokens[Math.Clamp(snapshot.TokenIndex, 0, tokens.Count - 1)];
                var currentTokenName = snapshot.TokenIndex < tokens.Count ? currentToken.LexicalUnit.Name : EndSymbol;
                var currentSymbol = snapshot.CloneStack.Peek();
                var syntaxProductions = m_predictiveAnylisisTable[currentSymbol][currentTokenName];
                if (snapshot.ChosenProductionIndex + 1 < syntaxProductions.Count)
                {
                    stack = new Stack<string>(snapshot.CloneStack.Reverse());
                    index = snapshot.TokenIndex;

                    var production = syntaxProductions[++snapshot.ChosenProductionIndex];

                    stack.Pop();
                    for (int i = production.Symbols.Count - 1; i >= 0; i--)
                        stack.Push(production.Symbols[i]);

                    return true;
                }
                else
                {
                    snapshotStack.Pop();
                }
            }
            stack = null;
            index = -1;
            return false;
        }

        class Snapshot
        {
            public int StackHeight;
            public Stack<string> CloneStack;
            public int TokenIndex;
            public int ChosenProductionIndex;

            public Snapshot(Stack<string> stack, int tokenIndex, int chosenProductionIndex)
            {
                StackHeight = stack.Count;
                CloneStack = new Stack<string>(stack.Reverse());
                TokenIndex = tokenIndex;
                ChosenProductionIndex = chosenProductionIndex;
            }
        }

        private void Initialize()
        {
            EliminateEmptyProduction();
            EliminateCircle();
            EliminateLeftRecursion();
            ExtractLeftCommonFactor();

            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                MyLogger.WriteLine(syntaxLine.ToString());
            }

            var firstSet = FirstSet();
            MyLogger.WriteLine("FirstSet:");
            foreach (var set in firstSet)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(set.Key);
                stringBuilder.Append(": ");
                foreach (var symbol in set.Value)
                {
                    stringBuilder.Append(symbol.ToString());
                    stringBuilder.Append(" ");
                }
                MyLogger.WriteLine(stringBuilder.ToString());
            }

            var followSet = FollowSet(firstSet);
            MyLogger.WriteLine("FollowSet:");
            foreach (var set in followSet)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(set.Key);
                stringBuilder.Append(": ");
                foreach (var symbol in set.Value)
                {
                    stringBuilder.Append(symbol.ToString());
                    stringBuilder.Append(" ");
                }
                MyLogger.WriteLine(stringBuilder.ToString());
            }

            if (!IsValidLL1())
            {
                throw new Exception();
            }

            m_predictiveAnylisisTable = PredictiveAnalysisTable(firstSet, followSet);
            PrintPredictiveAnalysisTable(m_predictiveAnylisisTable);
        }

        /// <summary>
        /// 消除空表达式
        /// </summary>
        private void EliminateEmptyProduction()
        {
            // TODO 
        }

        /// <summary>
        /// 消除环
        /// </summary>
        private void EliminateCircle()
        {
            // TODO
        }

        private bool IsEmptyProduction(Production production)
        {
            return production.Symbols.Count == 1 && production.Symbols[0].Equals(Helpers.EmptyOperator.ToString());
        }

        /// <summary>
        /// 消除左递归
        /// </summary>
        private void EliminateLeftRecursion()
        {
            // TODO 是否需要对syntaxLinesList排序？理论上是后面的文法表达式中会使用到前面的文法，前面的文法表达式中不使用后面的文法
            List<SyntaxLine> newSyntaxLines = new List<SyntaxLine>();
            var syntaxLinesList = m_syntaxLines.Values.ToList();
            for (int i = 0; i < syntaxLinesList.Count; i++)
            {
                Dictionary<Production, List<Production>> productionsToReplace = new Dictionary<Production, List<Production>>();
                for (int j = 0; j < i; j++)
                {
                    // 将每个形如Ai->AjY的产生式替换为产生式组Ai->X1Y|X2Y|...|XkY，
                    // 其中Aj->X1|X2|...|Xk是所有的Aj产生式
                    foreach (var production in syntaxLinesList[i].Productions)
                    {
                        if (production.Symbols[0] == syntaxLinesList[j].Name)
                        {
                            productionsToReplace.Add(production, new List<Production>());
                            List<Production> newProductions2 = syntaxLinesList[i].Productions;
                            foreach (var production2 in syntaxLinesList[j].Productions)
                            {
                                Production newProduction = new Production(syntaxLinesList[i].Name);
                                newProduction.Symbols.AddRange(production2.Symbols);
                                for (int k = 1; k < production.Symbols.Count; k++)
                                    newProduction.Symbols.Add(production.Symbols[k]);
                                productionsToReplace[production].Add(newProduction);
                            }
                        }
                    }
                }
                foreach (var toReplace in productionsToReplace)
                {
                    syntaxLinesList[i].Productions.Remove(toReplace.Key);
                    syntaxLinesList[i].Productions.AddRange(toReplace.Value);
                }
                // 消除新产生式的直接左递归
                SyntaxLine newSyntaxLine = new SyntaxLine();
                newSyntaxLine.Name = string.Format("{0}'", syntaxLinesList[i].Name);
                List<Production> notLeftRecursionProductions = new List<Production>();
                bool haveLeftRecursion = false;
                foreach (var production in syntaxLinesList[i].Productions)
                {
                    if (production.Symbols[0] == syntaxLinesList[i].Name)
                    {
                        haveLeftRecursion = true;
                        // 构建左递归表达式的替代表达式
                        Production production1 = new Production(newSyntaxLine.Name);
                        for (int j = 1; j < production.Symbols.Count; j++)
                            production1.Symbols.Add(production.Symbols[j]);
                        production1.Symbols.Add(newSyntaxLine.Name);
                        newSyntaxLine.Productions.Add(production1);
                    }
                    else
                    {
                        // 收集非左递归的表达式
                        notLeftRecursionProductions.Add(production);
                    }
                }
                if (haveLeftRecursion) // 只有存在左递归时，才对当前文法进行修改
                {
                    // 给生成的新文法添加空表达式，并收集生成的新文法
                    newSyntaxLine.Productions.Add(new Production(newSyntaxLine.Name)
                    {
                        Symbols = new List<string>() { Helpers.EmptyOperator.ToString() }
                    });
                    newSyntaxLines.Add(newSyntaxLine);
                    // 给当前修改的文法赋予新的表达式组
                    List<Production> eliminated = new List<Production>();
                    foreach (var aa in notLeftRecursionProductions)
                    {
                        Production newProduction = new Production(syntaxLinesList[i].Name);
                        newProduction.Symbols.AddRange(aa.Symbols);
                        newProduction.Symbols.Add(newSyntaxLine.Name);
                        eliminated.Add(newProduction);
                    }
                    syntaxLinesList[i].Productions = eliminated;
                }
            }
            // 将新产生的文法加入到文法列表中
            foreach (var newSyntaxLine in newSyntaxLines)
                AddNewSyntaxLine(newSyntaxLine);
        }

        #region 提取左公因子部分
        /// <summary>
        /// 提取左公因子
        /// </summary>
        private void ExtractLeftCommonFactor()
        {
            Dictionary<string, SyntaxLine> newSyntaxLines = new Dictionary<string, SyntaxLine>();
            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                for (int i = 0; i < syntaxLine.Productions.Count; i++)
                {
                    List<int> indexesHaveLeftCommonFactor = new List<int>();
                    indexesHaveLeftCommonFactor.Add(i);
                    List<string> leftCommonFactor = new List<string>();
                    GetLeftCommonFactorRecursively(syntaxLine, i, ref indexesHaveLeftCommonFactor, ref leftCommonFactor);
                    if (indexesHaveLeftCommonFactor.Count > 1)
                    {
                        SyntaxLine newSyntaxLine = new SyntaxLine();
                        newSyntaxLine.Name = string.Format("{0}'", syntaxLine.Name);
                        while (newSyntaxLines.ContainsKey(newSyntaxLine.Name))
                            newSyntaxLine.Name = string.Format("{0}'", newSyntaxLine.Name);

                        // 获取需要移除的具有左公因子的表达式，并生成新文法的所有表达式
                        List<Production> toRemoveProductions = new List<Production>();
                        List<Production> newProductionsForNew = new List<Production>();
                        foreach (var index in indexesHaveLeftCommonFactor)
                        {
                            var toRemove = syntaxLine.Productions[index];
                            toRemoveProductions.Add(toRemove);

                            if (leftCommonFactor.Count == toRemove.Symbols.Count)
                                newProductionsForNew.Add(new Production(newSyntaxLine.Name) { Symbols = new List<string>() { Helpers.EmptyOperator.ToString() } });
                            else
                            {
                                Production newProduction = new Production(newSyntaxLine.Name);
                                for (int j = leftCommonFactor.Count; j < toRemove.Symbols.Count; j++)
                                    newProduction.Symbols.Add(toRemove.Symbols[j]);
                                newProductionsForNew.Add(newProduction);
                            }
                        }

                        // 对旧的文法移除包含左公因子的表达式，并添加新替换的表达式
                        foreach (var toRemove in toRemoveProductions)
                            syntaxLine.Productions.Remove(toRemove);
                        Production newProductionForOld = new Production(syntaxLine.Name);
                        newProductionForOld.Symbols.AddRange(leftCommonFactor);
                        newProductionForOld.Symbols.Add(newSyntaxLine.Name);
                        syntaxLine.Productions.Add(newProductionForOld);

                        newSyntaxLine.Productions.AddRange(newProductionsForNew);
                        newSyntaxLines.Add(newSyntaxLine.Name, newSyntaxLine);
                    }
                }
            }
            foreach (var newSyntaxLine in newSyntaxLines.Values)
                AddNewSyntaxLine(newSyntaxLine);
        }

        private void GetLeftCommonFactorRecursively(SyntaxLine syntaxLine, int i, ref List<int> indexesHaveLeftCommonFactor, ref List<string> leftCommonFactor)
        {
            List<int> indexesOnStart = new List<int>(indexesHaveLeftCommonFactor);
            if (leftCommonFactor.Count == syntaxLine.Productions[i].Symbols.Count) // 如果左公因子个数已经达到当前的符号个数，则停止
                return;

            leftCommonFactor.Add(syntaxLine.Productions[i].Symbols[leftCommonFactor.Count]);
            for (int j = i + 1; j < syntaxLine.Productions.Count; j++)
            {
                if (IsPrefix(leftCommonFactor, syntaxLine.Productions[j].Symbols))
                {
                    if (!indexesHaveLeftCommonFactor.Contains(j))
                        indexesHaveLeftCommonFactor.Add(j);
                }
                else
                {
                    if (indexesHaveLeftCommonFactor.Contains(j))
                        indexesHaveLeftCommonFactor.Remove(j);
                }
            }
            if (indexesHaveLeftCommonFactor.Count == 1) // 直到只有一个表达式，则回溯一次，获取上一次的左公因子
            {
                indexesHaveLeftCommonFactor = indexesOnStart;
                leftCommonFactor.RemoveAt(leftCommonFactor.Count - 1);
                return;
            }
            if (indexesHaveLeftCommonFactor.Count > 1)
            {
                // 尝试左公因子+1，如果还有多个表达式，继续+1
                GetLeftCommonFactorRecursively(syntaxLine, i, ref indexesHaveLeftCommonFactor, ref leftCommonFactor);
            }
        }

        private bool IsPrefix(List<string> prefix, List<string> stringList)
        {
            for (int i = 0; i < prefix.Count; i++)
            {
                if (i >= stringList.Count || prefix[i] != stringList[i])
                    return false;
            }
            return true;
        }
        #endregion

        private void AddNewSyntaxLine(SyntaxLine newSyntaxLine)
        {
            m_syntaxLines.Add(newSyntaxLine.Name, newSyntaxLine);
        }

        #region 求first集部分

        /// <summary>
        /// 求First集
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, HashSet<string>> FirstSet()
        {
            Dictionary<string, HashSet<string>> firstSet = new Dictionary<string, HashSet<string>>();
            // 收集文法中所有符号
            HashSet<string> allSymbols = new HashSet<string>();
            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                if (!allSymbols.Contains(syntaxLine.Name))
                    allSymbols.Add(syntaxLine.Name);
                foreach (var production in syntaxLine.Productions)
                {
                    foreach (var symbol in production.Symbols)
                    {
                        if (!allSymbols.Contains(symbol))
                            allSymbols.Add(symbol);
                    }
                }
            }

            // TODO 在此之前应该除去文法中的环
            foreach (var symbol in allSymbols)
                SymbolFirstSetRecursively(firstSet, symbol);

            return firstSet;
        }

        private void SymbolFirstSetRecursively(Dictionary<string, HashSet<string>> firstSet, string symbol)
        {
            if (firstSet.ContainsKey(symbol))
                return;

            if (symbol.Equals(Helpers.EmptyOperator.ToString()))
                return;

            if (IsTerminalSymbol(symbol)) // 如果是终结符号
            {
                firstSet.Add(symbol, new HashSet<string> { symbol }); // 其first集是自己
            }
            else // 如果是非终结符号
            {
                firstSet.Add(symbol, new HashSet<string>());
                var syntaxLine = m_syntaxLines[symbol];
                foreach (var production in syntaxLine.Productions)
                {
                    bool haveEmptyProduction = true;
                    if (IsEmptyProduction(production))
                    {
                        firstSet[symbol].Add(Helpers.EmptyOperator.ToString());
                    }
                    else
                    {
                        foreach (var productionSymbol in production.Symbols)
                        {
                            if (!firstSet.ContainsKey(productionSymbol))
                                SymbolFirstSetRecursively(firstSet, productionSymbol);

                            if (haveEmptyProduction)
                                firstSet[symbol].UnionWith(firstSet[productionSymbol]);

                            if (!firstSet[productionSymbol].Contains(Helpers.EmptyOperator.ToString()))
                                haveEmptyProduction = false;
                        }
                    }
                }
            }
        }

        private bool IsTerminalSymbol(string symbol)
        {
            // 如果是文法中左侧出现的符号，则是非终结符
            // 因为任何一个非终结符，都必须在文法左侧定义，而不能只出现在文法右侧
            return !m_syntaxLines.Keys.Contains(symbol)
                && !symbol.Equals(Helpers.EmptyOperator.ToString())
                && !symbol.Equals(EndSymbol);
        }

        #endregion

        #region 求Follow集部分

        /// <summary>
        /// 求Follow集
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, HashSet<string>> FollowSet(Dictionary<string, HashSet<string>> firstSet)
        {
            Dictionary<string, HashSet<string>> followSet = new Dictionary<string, HashSet<string>>();

            foreach (var nonTernimalSymbol in m_syntaxLines.Keys)
                SymbolFollowSetRecursively(firstSet, followSet, nonTernimalSymbol);

            return followSet;
        }

        private void SymbolFollowSetRecursively(Dictionary<string, HashSet<string>> firstSet, Dictionary<string, HashSet<string>> followSet, string nonTernimalSymbol)
        {
            if (followSet.ContainsKey(nonTernimalSymbol))
                return;

            if (nonTernimalSymbol.Equals(StartSymbol))
                followSet.Add(nonTernimalSymbol, new HashSet<string>() { EndSymbol });
            else
            {
                followSet.Add(nonTernimalSymbol, new HashSet<string>());
                foreach (var syntaxLine in m_syntaxLines.Values)
                {
                    foreach (var production in syntaxLine.Productions)
                    {
                        for (int i = 0; i < production.Symbols.Count; i++)
                        {
                            var currentSymbol = production.Symbols[i];
                            if (currentSymbol.Equals(nonTernimalSymbol) && i + 1 < production.Symbols.Count)
                            {
                                var nextSymbol = production.Symbols[i + 1];
                                followSet[nonTernimalSymbol].UnionWith(firstSet[nextSymbol]);
                                if (followSet[nonTernimalSymbol].Contains(Helpers.EmptyOperator.ToString()))
                                    followSet[nonTernimalSymbol].Remove(Helpers.EmptyOperator.ToString());
                            }
                            if (currentSymbol.Equals(nonTernimalSymbol) && i + 1 == production.Symbols.Count
                                || i + 1 < production.Symbols.Count && firstSet[production.Symbols[i + 1]].Contains(Helpers.EmptyOperator.ToString()))
                            {
                                if (followSet.ContainsKey(syntaxLine.Name))
                                {
                                    SymbolFollowSetRecursively(firstSet, followSet, syntaxLine.Name);
                                    followSet[nonTernimalSymbol].UnionWith(followSet[syntaxLine.Name]);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 判断该文法是否是LL(1)的
        /// </summary>
        /// <returns></returns>
        private bool IsValidLL1()
        {
            // TODO
            return true;
        }

        private Dictionary<string, Dictionary<string, List<Production>>> PredictiveAnalysisTable(Dictionary<string, HashSet<string>> firstSet, Dictionary<string, HashSet<string>> followSet)
        {
            Dictionary<string, Dictionary<string, List<Production>>> table = new Dictionary<string, Dictionary<string, List<Production>>>();

            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                var currentRow = new Dictionary<string, List<Production>>();
                table.Add(syntaxLine.Name, currentRow);
                foreach (var production in syntaxLine.Productions)
                {
                    var firstSymbol = production.Symbols[0];
                    HashSet<string> currentProductionFirst;
                    if (firstSymbol.Equals(Helpers.EmptyOperator.ToString()))
                        currentProductionFirst = new HashSet<string>() { Helpers.EmptyOperator.ToString() };
                    else
                        currentProductionFirst = firstSet[firstSymbol];
                    foreach (var first in currentProductionFirst)
                    {
                        if (IsTerminalSymbol(first))
                            AddToCell(currentRow, first, production);
                    }
                    if (currentProductionFirst.Contains(Helpers.EmptyOperator.ToString()))
                    {
                        var currentSyntaxLineFollow = followSet[syntaxLine.Name];
                        foreach (var symbol in currentSyntaxLineFollow)
                        {
                            if (IsTerminalSymbol(symbol))
                                AddToCell(currentRow, symbol, production);
                        }
                        if (currentSyntaxLineFollow.Contains(EndSymbol))
                            AddToCell(currentRow, EndSymbol, production);
                    }
                }
            }

            return table;
        }

        private void AddToCell(Dictionary<string, List<Production>> currentRow, string symbol, Production production)
        {
            if (!currentRow.ContainsKey(symbol))
                currentRow.Add(symbol, new List<Production>() { production });
            else
            {
                if (!currentRow[symbol].Contains(production))
                    currentRow[symbol].Add(production);
            }

            currentRow[symbol].Sort((production1, production2) =>
            {
                if (IsEmptyProduction(production1))
                    return 1;
                else if (IsEmptyProduction(production2))
                    return -1;
                return 0;
            });
        }

        private void PrintPredictiveAnalysisTable(Dictionary<string, Dictionary<string, List<Production>>> table)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var pair1 in table)
            {
                stringBuilder.AppendLine(pair1.Key);
                foreach (var pair2 in pair1.Value)
                {
                    for (int i = 0; i < pair1.Key.Length; i++)
                        stringBuilder.Append(" ");
                    stringBuilder.Append(pair2.Key);
                    stringBuilder.AppendLine();
                    for (int i = 0; i < pair2.Value.Count; i++)
                    {
                        for (int j = 0; j < pair1.Key.Length + pair2.Key.Length; j++)
                            stringBuilder.Append(" ");
                        stringBuilder.Append(pair2.Value[i]);
                        stringBuilder.AppendLine();
                    }
                }
                stringBuilder.AppendLine();
            }
            MyLogger.WriteLine(stringBuilder.ToString());
        }

        internal class Result
        {
            public List<CompileError> Errors = new List<CompileError>();

            public void AppendError(CompileError error)
            {
                Errors.Add(error);
            }
        }
    }
}

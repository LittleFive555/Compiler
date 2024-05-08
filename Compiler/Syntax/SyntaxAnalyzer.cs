using System.Text;
using Compiler.Lexical;
using Compiler.Syntax.Model;

namespace Compiler.Syntax
{
    internal class SyntaxAnalyzer
    {
        private readonly Dictionary<string, SyntaxLine> m_syntaxLines;
        private Dictionary<string, Dictionary<string, List<Production>>> m_predictiveAnylisisTable;

        private const string StartSymbol = "S";
        private const string EndSymbol = "$";
        private readonly SyntaxUnit StartSyntaxUnit = new SymbolName(StartSymbol);
        private readonly SyntaxUnit EndSyntaxUnit = new SymbolName(EndSymbol);

        public SymbolTable SymbolTable { get; } = new SymbolTable();

        private Stack<Scope> m_scopeStack = new Stack<Scope>();

        public Scope CurrentScope => m_scopeStack.Peek();

        public SyntaxAnalyzer(Dictionary<string, SyntaxLine> syntaxLines)
        {
            m_syntaxLines = syntaxLines;
            if (!m_syntaxLines.ContainsKey(StartSymbol))
                throw new Exception("没有文法开始符号 S ，请检查是否有产生式左侧命名为 S 的文法");

            PrintSyntaxLines("Read");

            EliminateEmptyProduction();

            EliminateCircle();

            EliminateLeftRecursion();
            PrintSyntaxLines("After EliminateLeftRecursion");

            ExtractLeftCommonFactor(m_syntaxLines.Values);
            PrintSyntaxLines("After ExtractLeftCommonFactor");

            var firstSet = FirstSet();
            PrintFirstOrFollowSet("First", firstSet);

            var followSet = FollowSet(firstSet);
            PrintFirstOrFollowSet("Follow", followSet);

            if (!IsValidLL1())
            {
                throw new Exception();
            }

            m_predictiveAnylisisTable = PredictiveAnalysisTable(firstSet, followSet);
            PrintPredictiveAnalysisTable(m_predictiveAnylisisTable);

            PushScope(new Scope());
        }

        public Result Execute(Uri documentUri, List<Token> tokens)
        {
            Result result = new Result();

            Stack<Snapshot> snapshotStack = new Stack<Snapshot>();
            Stack<ParseAction> actionStack = new Stack<ParseAction>();
            Stack<SyntaxUnit> stack = new Stack<SyntaxUnit>();
            stack.Push(EndSyntaxUnit);
            stack.Push(StartSyntaxUnit);

            int index = 0;
            SyntaxUnit currentSyntaxUnit = stack.Peek();
            int counter = 0;
            while (currentSyntaxUnit != EndSyntaxUnit || index < tokens.Count)
            {
                counter = PrintAnalyzeProgress(tokens, stack, index, counter);

                if (tokens[Math.Clamp(index, 0, tokens.Count - 1)].LexicalUnit.LexicalType == LexicalType.Comment)
                {
                    index++;
                    continue;
                }

                var currentToken = tokens[Math.Clamp(index, 0, tokens.Count - 1)];
                string currentTokenName = index < tokens.Count ? currentToken.LexicalUnit.Name : EndSymbol;

                if (currentSyntaxUnit.SyntaxUnitType == SyntaxUnitType.SymbolName)
                {
                    if (index < tokens.Count && currentSyntaxUnit == EndSyntaxUnit)
                    {
                        if (TryRecall(tokens, snapshotStack, actionStack, out var tempStack, out var tempIndex))
                        {
                            stack = tempStack;
                            index = tempIndex;
                        }
                        else
                        {
                            result.AppendError(new CompileError(currentToken.Line, currentToken.StartColumn, currentToken.Length, string.Format("Expect \"{0}\"", currentSyntaxUnit)));
                            // TODO try fix and continue
                            index++;
                        }
                    }
                    else if (currentSyntaxUnit.Content == currentTokenName)
                    {
                        stack.Pop();
                        while (snapshotStack.Count > 0 && snapshotStack.Peek().CanRemove(stack))
                            snapshotStack.Pop();
                        index++;
                    }
                    else if (IsTerminalSymbol(currentSyntaxUnit.Content))
                    {
                        if (TryRecall(tokens, snapshotStack, actionStack, out var tempStack, out var tempIndex))
                        {
                            stack = tempStack;
                            index = tempIndex;
                        }
                        else
                        {
                            result.AppendError(new CompileError(currentToken.Line, currentToken.StartColumn, currentToken.Length, string.Format("Expect \"{0}\"", currentSyntaxUnit)));
                            // TODO try fix and continue
                            stack.Pop();
                        }
                    }
                    else if (!m_predictiveAnylisisTable[currentSyntaxUnit.Content].ContainsKey(currentTokenName))
                    {
                        if (TryRecall(tokens, snapshotStack, actionStack, out var tempStack, out var tempIndex))
                        {
                            stack = tempStack;
                            index = tempIndex;
                        }
                        else
                        {
                            result.AppendError(new CompileError(currentToken.Line, currentToken.StartColumn, currentToken.Length, string.Format("Expect \"{0}\"", currentSyntaxUnit)));
                            // TODO try fix and continue
                            index++;
                        }
                    }
                    else
                    {
                        var syntaxProductions = m_predictiveAnylisisTable[currentSyntaxUnit.Content][currentTokenName];
                        //if (syntaxProductions.Count > 2)
                        //    throw new Exception();

                        Production production;
                        if (syntaxProductions.Count == 1) // 只有一个产生式，选择该产生式
                            production = syntaxProductions[0];
                        else if (syntaxProductions.Count == 2)
                        {
                            if (IsEmptyProduction(syntaxProductions[1])) // 第一个产生式非空，第二个产生式为空，选择非空
                                production = syntaxProductions[0];
                            else // 两个产生式全非空，则准备回溯（或许可能会当有空产生式时也回溯？）
                            {
                                var snapshot = new Snapshot(stack, index, 0, actionStack.Count);
                                snapshotStack.Push(snapshot);
                                production = syntaxProductions[0];
                            }
                        }
                        else
                        {
                            var snapshot = new Snapshot(stack, index, 0, actionStack.Count);
                            snapshotStack.Push(snapshot);
                            production = syntaxProductions[0];
                        }

                        if (IsEmptyProduction(production))
                        {
                            stack.Pop();
                        }
                        else
                        {
                            stack.Pop();
                            for (int i = production.SyntaxUnitList.Count - 1; i >= 0; i--)
                                stack.Push(production.SyntaxUnitList[i]);
                        }
                    }
                }
                else if (currentSyntaxUnit.SyntaxUnitType == SyntaxUnitType.ParseAction)
                {
                    var parseAction = currentSyntaxUnit as ParseAction;
                    parseAction.Execute(this, new ParserContext(documentUri, currentToken));
                    actionStack.Push(parseAction);
                    stack.Pop();
                }
                currentSyntaxUnit = stack.Peek();
            }
            MyLogger.WriteLine("");
            MyLogger.WriteLine("");
            MyLogger.WriteLine("Symbol Table:");
            MyLogger.WriteLine(SymbolTable.ToString());
            return result;
        }

        private bool TryRecall(List<Token> tokens, Stack<Snapshot> snapshotStack, Stack<ParseAction> parseActionStack, out Stack<SyntaxUnit> stack, out int index)
        {
            while (snapshotStack.Count > 0)
            {
                var snapshot = snapshotStack.Peek();
                var currentToken = tokens[Math.Clamp(snapshot.TokenIndex, 0, tokens.Count - 1)];
                var currentTokenName = snapshot.TokenIndex < tokens.Count ? currentToken.LexicalUnit.Name : EndSymbol;
                var currentSymbol = snapshot.CloneStack.Peek();
                var syntaxProductions = m_predictiveAnylisisTable[currentSymbol.Content][currentTokenName];
                if (snapshot.ChosenProductionIndex + 1 < syntaxProductions.Count)
                {

                    var production = syntaxProductions[++snapshot.ChosenProductionIndex];
                    if (IsEmptyProduction(production))
                    {
                        snapshotStack.Pop();
                    }
                    else
                    {
                        stack = new Stack<SyntaxUnit>(snapshot.CloneStack.Reverse());
                        index = snapshot.TokenIndex;
                        while (parseActionStack.Count != snapshot.ParseActionStackCount)
                        {
                            var action = parseActionStack.Pop();
                            action.RevertExecute(this);
                        }

                        stack.Pop();
                        for (int i = production.SyntaxUnitList.Count - 1; i >= 0; i--)
                            stack.Push(production.SyntaxUnitList[i]);

                        return true;
                    }
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

        private static bool IsEmptyProduction(Production production)
        {
            return production.SyntaxUnitList.Count == 1 && production.SyntaxUnitList[0].Content == Helpers.EmptyOperator.ToString();
        }

        #region 消除左递归部分

        /// <summary>
        /// 消除左递归
        /// </summary>
        private void EliminateLeftRecursion()
        {
            Dictionary<string, SyntaxLine> newSyntaxLines = new Dictionary<string, SyntaxLine>();
            var syntaxLinesList = SortSyntaxLine(m_syntaxLines);
            for (int i = 0; i < syntaxLinesList.Count; i++)
            {
                var currentSyntaxLine = syntaxLinesList[i];
                ReplaceFirstSymbol(currentSyntaxLine, syntaxLinesList.GetRange(0, i));
                // 消除新产生式的直接左递归
                List<Production> notLeftRecursionProductions = new List<Production>();
                bool haveLeftRecursion = false;
                string name = GetNewSyntaxLineName(newSyntaxLines, currentSyntaxLine.Name);
                List<Production> productions = new List<Production>();
                foreach (var production in currentSyntaxLine.Productions)
                {
                    if (production.SyntaxUnitList[0].Content == currentSyntaxLine.Name)
                    {
                        haveLeftRecursion = true;
                        // 构建左递归表达式的替代表达式
                        List<SyntaxUnit> symbols = new List<SyntaxUnit>();
                        for (int j = 1; j < production.SyntaxUnitList.Count; j++)
                            symbols.Add(production.SyntaxUnitList[j]);
                        symbols.Add(new SymbolName(name));
                        productions.Add(new Production(symbols));
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
                    productions.Add(new Production(new List<SyntaxUnit>() { new SymbolName(Helpers.EmptyOperator.ToString()) }));
                    newSyntaxLines.Add(name, new SyntaxLine(name, productions));
                    // 给当前修改的文法赋予新的表达式组
                    List<Production> eliminated = new List<Production>();
                    foreach (var production in notLeftRecursionProductions)
                    {
                        List<SyntaxUnit> symbols = new List<SyntaxUnit>();
                        symbols.AddRange(production.SyntaxUnitList);
                        symbols.Add(new SymbolName(name));
                        eliminated.Add(new Production(symbols));
                    }
                    currentSyntaxLine.SetProductions(eliminated);
                }
            }
            // 将新产生的文法加入到文法列表中
            foreach (var newSyntaxLine in newSyntaxLines.Values)
                AddNewSyntaxLine(newSyntaxLine);
        }

        private static SyntaxLine ReplaceFirstSymbol(SyntaxLine currentSyntaxLine, IReadOnlyList<SyntaxLine> syntaxLinesBeforeCurrent)
        {
            Dictionary<Production, List<Production>> productionsToReplace = new Dictionary<Production, List<Production>>();
            for (int i = 0; i < syntaxLinesBeforeCurrent.Count; i++)
            {
                // 将每个形如Ai->AjY的产生式替换为产生式组Ai->X1Y|X2Y|...|XkY，
                // 其中Aj->X1|X2|...|Xk是所有的Aj产生式
                foreach (var production in currentSyntaxLine.Productions)
                {
                    List<SyntaxUnit> tempList = new List<SyntaxUnit>();
                    List<SyntaxUnit> symbols = new List<SyntaxUnit>(production.SyntaxUnitList);
                    for (int j = 0; j < symbols.Count; j++)
                    {
                        if (symbols[j].SyntaxUnitType == SyntaxUnitType.ParseAction)
                        {
                            tempList.Add(symbols[j]);
                            symbols.RemoveAt(j);
                            j--;
                        }
                        else
                            break;
                    }
                    if (symbols[0].Content == syntaxLinesBeforeCurrent[i].Name)
                    {
                        productionsToReplace.Add(production, new List<Production>());
                        foreach (var production2 in syntaxLinesBeforeCurrent[i].Productions)
                        {
                            List<SyntaxUnit> newSymbolsList = new List<SyntaxUnit>();
                            newSymbolsList.AddRange(tempList);
                            if (!IsEmptyProduction(production2) || production.SyntaxUnitList.Count < 2)
                                newSymbolsList.AddRange(production2.SyntaxUnitList);
                            for (int k = 1; k < production.SyntaxUnitList.Count; k++)
                                newSymbolsList.Add(production.SyntaxUnitList[k]);
                            Production newProduction = new Production(newSymbolsList);

                            // 如果产生了完全相同的产生式，则跳过
                            bool haveSame = false;
                            foreach (var temp in productionsToReplace[production])
                            {
                                if (Production.IsSameSymbolsList(temp, newProduction))
                                {
                                    haveSame = true;
                                    break;
                                }
                            }
                            foreach (var temp in currentSyntaxLine.Productions)
                            {
                                if (Production.IsSameSymbolsList(temp, newProduction))
                                {
                                    haveSame = true;
                                    break;
                                }
                            }
                            if (!haveSame)
                                productionsToReplace[production].Add(newProduction);
                        }
                    }
                }
            }
            List<Production> newProductions = new List<Production>(currentSyntaxLine.Productions);
            foreach (var toReplace in productionsToReplace)
            {
                newProductions.Remove(toReplace.Key);
                newProductions.AddRange(toReplace.Value);
                // 移除多余的空表达式
                bool haveOneEmpty = false;
                for (int i = 0; i < currentSyntaxLine.Productions.Count; i++)
                {
                    if (IsEmptyProduction(currentSyntaxLine.Productions[i]))
                    {
                        if (!haveOneEmpty)
                            haveOneEmpty = true;
                        else
                        {
                            newProductions.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            currentSyntaxLine.SetProductions(newProductions);
            return currentSyntaxLine;
        }

        private static List<SyntaxLine> SortSyntaxLine(Dictionary<string, SyntaxLine> syntaxLinesDic)
        {
            Dictionary<string, List<string>> beUsedDic = new Dictionary<string, List<string>>();
            Dictionary<string, int> beUsedWeight = new Dictionary<string, int>();
            foreach (var syntaxLine in syntaxLinesDic)
                beUsedDic.Add(syntaxLine.Key, new List<string>());
            foreach (var syntaxLine in syntaxLinesDic)
            {
                foreach (var production in syntaxLine.Value.Productions)
                {
                    string firstSymbolName = production.SyntaxUnitList[0].Content;
                    if (syntaxLinesDic.ContainsKey(firstSymbolName)) // 非终结符
                    {
                        if (firstSymbolName == syntaxLine.Key)
                            continue;

                        if (!beUsedDic[firstSymbolName].Contains(syntaxLine.Key))
                            beUsedDic[firstSymbolName].Add(syntaxLine.Key);
                    }
                }
            }

            foreach (var beused in beUsedDic)
                IterationCalculateWeight(0, beUsedDic, beUsedWeight, beused.Key);

            List<SyntaxLine> sortedList = syntaxLinesDic.Values.ToList();
            sortedList.Sort((syntaxLine1, syntaxLine2) => beUsedWeight[syntaxLine2.Name] - beUsedWeight[syntaxLine1.Name]);
            return sortedList;
        }

        private static int IterationCalculateWeight(int weight, Dictionary<string, List<string>> beUsedDic, Dictionary<string, int> beUsedWeight, string currentSyntaxName)
        {
            weight += 1;
            if (beUsedWeight.ContainsKey(currentSyntaxName))
                return beUsedWeight[currentSyntaxName];
            else
            {
                if (beUsedDic[currentSyntaxName].Count > 0)
                {
                    foreach (var syntaxName in beUsedDic[currentSyntaxName])
                        weight += IterationCalculateWeight(weight, beUsedDic, beUsedWeight, syntaxName);
                    beUsedWeight.Add(currentSyntaxName, weight);
                }
                else
                {
                    beUsedWeight.Add(currentSyntaxName, weight);
                    return weight;
                }
            }

            return weight;
        }
        #endregion

        #region 提取左公因子部分
        /// <summary>
        /// 提取左公因子
        /// </summary>
        private void ExtractLeftCommonFactor(IEnumerable<SyntaxLine> syntaxLinesList)
        {
            Dictionary<string, SyntaxLine> newSyntaxLines = new Dictionary<string, SyntaxLine>();
            foreach (var syntaxLine in syntaxLinesList)
            {
                for (int i = 0; i < syntaxLine.Productions.Count; i++)
                {
                    List<int> indexesHaveLeftCommonFactor = new List<int>();
                    indexesHaveLeftCommonFactor.Add(i);
                    List<SyntaxUnit> leftCommonFactor = new List<SyntaxUnit>();
                    GetLeftCommonFactorRecursively(syntaxLine, i, ref indexesHaveLeftCommonFactor, ref leftCommonFactor);
                    if (indexesHaveLeftCommonFactor.Count > 1)
                    {
                        i--;

                        string name = GetNewSyntaxLineName(newSyntaxLines, syntaxLine.Name);
                        // 获取需要移除的具有左公因子的表达式，并生成新文法的所有表达式
                        List<Production> productionsToRemove = new List<Production>();
                        List<Production> productionsForNew = new List<Production>();
                        foreach (var index in indexesHaveLeftCommonFactor)
                        {
                            var toRemove = syntaxLine.Productions[index];
                            productionsToRemove.Add(toRemove);

                            if (leftCommonFactor.Count == toRemove.SyntaxUnitList.Count)
                                productionsForNew.Add(new Production(new List<SyntaxUnit>() { new SymbolName(Helpers.EmptyOperator.ToString()) }));
                            else
                            {
                                List<SyntaxUnit> symbolsList = new List<SyntaxUnit>();
                                for (int j = leftCommonFactor.Count; j < toRemove.SyntaxUnitList.Count; j++)
                                    symbolsList.Add(toRemove.SyntaxUnitList[j]);
                                productionsForNew.Add(new Production(symbolsList));
                            }
                        }
                        SyntaxLine newSyntaxLine = new SyntaxLine(name, productionsForNew);

                        // 对旧的文法移除包含左公因子的表达式，并添加新替换的表达式
                        SyntaxLine withSameProductions = GetSyntaxLineWithSameProductions(newSyntaxLines, newSyntaxLine);
                        List<SyntaxUnit> symbols = new List<SyntaxUnit>();
                        symbols.AddRange(leftCommonFactor);
                        if (withSameProductions == null)
                        {
                            symbols.Add(new SymbolName(name));
                            newSyntaxLines.Add(newSyntaxLine.Name, newSyntaxLine);
                        }
                        else
                        {
                            symbols.Add(new SymbolName(withSameProductions.Name));
                        }
                        Production newProductionForOld = new Production(symbols);
                        List<Production> productionsForOld = new List<Production>(syntaxLine.Productions);
                        foreach (var toRemove in productionsToRemove)
                            productionsForOld.Remove(toRemove);
                        productionsForOld.Add(newProductionForOld);
                        syntaxLine.SetProductions(productionsForOld);
                    }
                }
            }
            foreach (var newSyntaxLine in newSyntaxLines.Values)
                AddNewSyntaxLine(newSyntaxLine);

            if (newSyntaxLines.Count > 0)
                ExtractLeftCommonFactor(newSyntaxLines.Values);
        }

        private SyntaxLine GetSyntaxLineWithSameProductions(IReadOnlyDictionary<string, SyntaxLine> newSyntaxLines, SyntaxLine newSyntaxLine)
        {
            SyntaxLine withSameProductions = null;
            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                if (SyntaxLine.IsSameProductions(syntaxLine, newSyntaxLine))
                {
                    withSameProductions = syntaxLine;
                    break;
                }
            }
            if (withSameProductions == null)
            {
                foreach (var syntaxLine in newSyntaxLines.Values)
                {
                    if (SyntaxLine.IsSameProductions(syntaxLine, newSyntaxLine))
                    {
                        withSameProductions = syntaxLine;
                        break;
                    }
                }
            }

            return withSameProductions;
        }

        private void GetLeftCommonFactorRecursively(SyntaxLine syntaxLine, int i, ref List<int> indexesHaveLeftCommonFactor, ref List<SyntaxUnit> leftCommonFactor)
        {
            List<int> indexesOnStart = new List<int>(indexesHaveLeftCommonFactor);
            if (leftCommonFactor.Count == syntaxLine.Productions[i].SyntaxUnitList.Count) // 如果左公因子个数已经达到当前的符号个数，则停止
                return;

            leftCommonFactor.Add(syntaxLine.Productions[i].SyntaxUnitList[leftCommonFactor.Count]);
            for (int j = i + 1; j < syntaxLine.Productions.Count; j++)
            {
                if (IsPrefix(leftCommonFactor, syntaxLine.Productions[j].SyntaxUnitList))
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

        private bool IsPrefix(IReadOnlyList<SyntaxUnit> prefix, IReadOnlyList<SyntaxUnit> syntaxUnitList)
        {
            for (int i = 0; i < prefix.Count; i++)
            {
                if (i >= syntaxUnitList.Count || prefix[i] != syntaxUnitList[i])
                    return false;
            }
            return true;
        }
        #endregion

        private void AddNewSyntaxLine(SyntaxLine newSyntaxLine)
        {
            m_syntaxLines.Add(newSyntaxLine.Name, newSyntaxLine);
        }

        private string GetNewSyntaxLineName(IReadOnlyDictionary<string, SyntaxLine> newSyntaxLines, string name)
        {
            int count = 0;
            string newName = name;
            while (m_syntaxLines.ContainsKey(newName) || newSyntaxLines.ContainsKey(newName))
            {
                newName = string.Format("{0}_{1}", name, count);
                count++;
            }
            return newName;
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
                    foreach (var symbol in production.SyntaxUnitList)
                    {
                        if (symbol.SyntaxUnitType == SyntaxUnitType.SymbolName && !allSymbols.Contains(symbol.Content))
                            allSymbols.Add(symbol.Content);
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

            if (symbol == Helpers.EmptyOperator.ToString())
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
                        foreach (var syntaxUnit in production.SyntaxUnitList)
                        {
                            if (syntaxUnit.SyntaxUnitType != SyntaxUnitType.SymbolName)
                                continue;

                            string symbolName = syntaxUnit.Content;
                            if (!firstSet.ContainsKey(symbolName))
                                SymbolFirstSetRecursively(firstSet, symbolName);

                            if (haveEmptyProduction)
                                firstSet[symbol].UnionWith(firstSet[symbolName]);

                            if (!firstSet[symbolName].Contains(Helpers.EmptyOperator.ToString()))
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
                && symbol != Helpers.EmptyOperator.ToString()
                && symbol != EndSymbol;
        }

        #endregion

        #region 求Follow集部分

        /// <summary>
        /// 求Follow集
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, HashSet<string>> FollowSet(Dictionary<string, HashSet<string>> firstSet)
        {
            Dictionary<string, HashSet<string>> followSet = new Dictionary<string, HashSet<string>>
            {
                { StartSymbol, new HashSet<string>() { EndSymbol } }
            };

            bool haveNew = true;
            while (haveNew)
            {
                haveNew = false;
                foreach (var nonTerminalSymbol in m_syntaxLines.Keys)
                {
                    if (!followSet.ContainsKey(nonTerminalSymbol))
                        followSet.Add(nonTerminalSymbol, new HashSet<string>());
                    foreach (var syntaxLine in m_syntaxLines.Values)
                    {
                        foreach (var production in syntaxLine.Productions)
                        {
                            for (int i = 0; i < production.SyntaxUnitList.Count; i++)
                            {
                                var syntaxUnit = production.SyntaxUnitList[i];
                                if (syntaxUnit.SyntaxUnitType != SyntaxUnitType.SymbolName)
                                    continue;

                                SyntaxUnit? nextSymbolName = null;
                                int offset = 1;
                                while (i + offset < production.SyntaxUnitList.Count)
                                {
                                    if (production.SyntaxUnitList[i + offset].SyntaxUnitType == SyntaxUnitType.SymbolName)
                                    {
                                        nextSymbolName = production.SyntaxUnitList[i + offset];
                                        break;
                                    }
                                    offset++;
                                }
                                if (syntaxUnit.Content == nonTerminalSymbol && nextSymbolName != null)
                                {
                                    var firstSetWithoutEmpty = new HashSet<string>(firstSet[nextSymbolName.Content]);
                                    firstSetWithoutEmpty.ExceptWith(new HashSet<string>() { Helpers.EmptyOperator.ToString() });
                                    if (!followSet[nonTerminalSymbol].IsSupersetOf(firstSetWithoutEmpty))
                                    {
                                        haveNew = true;
                                        followSet[nonTerminalSymbol].UnionWith(firstSetWithoutEmpty);
                                    }
                                }
                                if ((syntaxUnit.Content == nonTerminalSymbol && i + 1 == production.SyntaxUnitList.Count)
                                    || (nextSymbolName != null && firstSet[nextSymbolName.Content].Contains(Helpers.EmptyOperator.ToString())))
                                {
                                    if (followSet.ContainsKey(syntaxLine.Name))
                                    {
                                        if (!followSet[nonTerminalSymbol].IsSupersetOf(followSet[syntaxLine.Name]))
                                        {
                                            haveNew = true;
                                            followSet[nonTerminalSymbol].UnionWith(followSet[syntaxLine.Name]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return followSet;
        }

        #endregion

        #region Parser相关部分

        public void PushScope(Scope scope)
        {
            m_scopeStack.Push(scope);
        }

        public Scope PopScope()
        {
            return m_scopeStack.Pop();
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
        
        #region 求预测分析表部分

        private Dictionary<string, Dictionary<string, List<Production>>> PredictiveAnalysisTable(Dictionary<string, HashSet<string>> firstSet, Dictionary<string, HashSet<string>> followSet)
        {
            Dictionary<string, Dictionary<string, List<Production>>> table = new Dictionary<string, Dictionary<string, List<Production>>>();

            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                var currentRow = new Dictionary<string, List<Production>>();
                table.Add(syntaxLine.Name, currentRow);
                foreach (var production in syntaxLine.Productions)
                {
                    HashSet<string> currentProductionFirst = new HashSet<string>();
                    if (IsEmptyProduction(production))
                        currentProductionFirst.Add(Helpers.EmptyOperator.ToString());
                    else
                    {
                        foreach (var symbol in production.SyntaxUnitList)
                        {
                            if (symbol.SyntaxUnitType != SyntaxUnitType.SymbolName)
                                continue;

                            currentProductionFirst.UnionWith(firstSet[symbol.Content]);
                            if (!firstSet[symbol.Content].Contains(Helpers.EmptyOperator.ToString()))
                                break;
                        }
                        foreach (var first in currentProductionFirst)
                        {
                            if (IsTerminalSymbol(first))
                                AddToCell(currentRow, first, production);
                        }
                    }
                    if (currentProductionFirst.Contains(Helpers.EmptyOperator.ToString()))
                    {
                        var currentSyntaxLineFollow = followSet[syntaxLine.Name];
                        foreach (var symbolInFollow in currentSyntaxLineFollow)
                        {
                            if (IsTerminalSymbol(symbolInFollow))
                                AddToCell(currentRow, symbolInFollow, production);
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

        #endregion

        private void PrintSyntaxLines(string title)
        {
            MyLogger.WriteLine("");
            MyLogger.WriteLine("");
            MyLogger.WriteLine(title);
            foreach (var syntaxLine in m_syntaxLines.Values)
                MyLogger.WriteLine(syntaxLine.ToString());
        }

        private static void PrintFirstOrFollowSet(string title, Dictionary<string, HashSet<string>> followSet)
        {
            MyLogger.WriteLine("");
            MyLogger.WriteLine("");
            MyLogger.WriteLine(title);
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
        }

        private void PrintPredictiveAnalysisTable(Dictionary<string, Dictionary<string, List<Production>>> table)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("PredictiveAnalysisTable");
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

        private static int PrintAnalyzeProgress(List<Token> tokens, Stack<SyntaxUnit> stack, int index, int counter)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("");
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
            return counter;
        }

        internal class Result
        {
            public List<CompileError> Errors = new List<CompileError>();

            public void AppendError(CompileError error)
            {
                Errors.Add(error);
            }
        }

        private class Snapshot
        {
            public Stack<SyntaxUnit> CloneStack;
            public int TokenIndex;
            public int ChosenProductionIndex;
            public int ParseActionStackCount;

            public Snapshot(Stack<SyntaxUnit> stack, int tokenIndex, int chosenProductionIndex, int parseActionStackCount)
            {
                CloneStack = new Stack<SyntaxUnit>(stack.Reverse());
                var temp1 = CloneStack.Pop();
                var temp2 = CloneStack.Pop();
                CloneStack.Push(temp2);
                CloneStack.Push(temp1);
                TokenIndex = tokenIndex;
                ChosenProductionIndex = chosenProductionIndex;
                ParseActionStackCount = parseActionStackCount;
            }

            public bool CanRemove(Stack<SyntaxUnit> stack)
            {
                if (stack.Count <= CloneStack.Count - 2)
                {
                    Stack<SyntaxUnit> temp1 = new Stack<SyntaxUnit>(stack.Reverse());
                    Stack<SyntaxUnit> temp2 = new Stack<SyntaxUnit>(CloneStack.Reverse());

                    while (temp2.Count > temp1.Count)
                        temp2.Pop();

                    bool isSame = true;
                    while (isSame && temp1.Count > 0)
                    {
                        if (temp1.Pop() != temp2.Pop())
                            isSame = false;
                    }
                    return isSame;
                }
                else
                    return false;
            }
        }
    }
}

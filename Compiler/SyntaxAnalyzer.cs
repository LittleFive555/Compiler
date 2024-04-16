using System.Text;

namespace Compiler
{
    public class SyntaxAnalyzer
    {
        private readonly List<Token> m_tokens;
        private readonly Dictionary<string, SyntaxLine> m_syntaxLines;

        private const string StartSymbol = "S";
        private const string EndSymbol = "$";

        public SyntaxAnalyzer(List<Token> tokens, Dictionary<string, SyntaxLine> syntaxLines)
        {
            m_tokens = tokens;
            m_syntaxLines = syntaxLines;
            if (!m_syntaxLines.ContainsKey(StartSymbol))
                throw new Exception("没有文法开始符号 S ，请检查是否有产生式左侧命名为 S 的文法");
        }

        public void Execute()
        {
            EliminateEmptyProduction();
            EliminateCircle();
            EliminateLeftRecursion();
            ExtractLeftCommonFactor();

            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                Console.WriteLine(syntaxLine.ToString());
            }

            var firstSet = FirstSet();
            Console.WriteLine("FirstSet:");
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
                Console.WriteLine(stringBuilder.ToString());
            }

            var followSet = FollowSet(firstSet);
            Console.WriteLine("FollowSet:");
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
                Console.WriteLine(stringBuilder.ToString());
            }
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
                                Production newProduction = new Production();
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
                        Production production1 = new Production();
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
                    newSyntaxLine.Productions.Add(new Production() { Symbols = new List<string>() { Helpers.EmptyOperator.ToString() } });
                    newSyntaxLines.Add(newSyntaxLine);
                    // 给当前修改的文法赋予新的表达式组
                    List<Production> eliminated = new List<Production>();
                    foreach (var aa in notLeftRecursionProductions)
                    {
                        Production newProduction = new Production();
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

        /// <summary>
        /// 提取左公因子
        /// </summary>
        private void ExtractLeftCommonFactor()
        {
            List<SyntaxLine> newSyntaxLines = new List<SyntaxLine>();
            foreach (var syntaxLine in m_syntaxLines.Values)
            {
                for (int i = 0; i < syntaxLine.Productions.Count; i++)
                {
                    List<int> indexesHaveLeftCommonFactor = new List<int>();
                    indexesHaveLeftCommonFactor.Add(i);
                    List<string> leftCommonFactor = new List<string>();
                    Func1(syntaxLine, i, ref indexesHaveLeftCommonFactor, ref leftCommonFactor);
                    if (indexesHaveLeftCommonFactor.Count > 1)
                    {
                        SyntaxLine newSyntaxLine = new SyntaxLine();
                        newSyntaxLine.Name = string.Format("{0}'", syntaxLine.Name);

                        // 获取需要移除的具有左公因子的表达式，并生成新文法的所有表达式
                        List<Production> toRemoveProductions = new List<Production>();
                        List<Production> newProductionsForNew = new List<Production>();
                        foreach (var index in indexesHaveLeftCommonFactor)
                        {
                            var toRemove = syntaxLine.Productions[index];
                            toRemoveProductions.Add(toRemove);

                            if (leftCommonFactor.Count == toRemove.Symbols.Count)
                                newProductionsForNew.Add(new Production() { Symbols = new List<string>() { Helpers.EmptyOperator.ToString() } });
                            else
                            {
                                Production newProduction = new Production();
                                for (int j = leftCommonFactor.Count; j < toRemove.Symbols.Count; j++)
                                    newProduction.Symbols.Add(toRemove.Symbols[j]);
                                newProductionsForNew.Add(newProduction);
                            }
                        }

                        // 对旧的文法移除包含左公因子的表达式，并添加新替换的表达式
                        foreach (var toRemove in toRemoveProductions)
                            syntaxLine.Productions.Remove(toRemove);
                        Production newProductionForOld = new Production();
                        newProductionForOld.Symbols.AddRange(leftCommonFactor);
                        newProductionForOld.Symbols.Add(newSyntaxLine.Name);
                        syntaxLine.Productions.Add(newProductionForOld);

                        newSyntaxLine.Productions.AddRange(newProductionsForNew);
                        newSyntaxLines.Add(newSyntaxLine);
                    }
                }
            }
            foreach (var newSyntaxLine in newSyntaxLines)
                AddNewSyntaxLine(newSyntaxLine);
        }

        private void AddNewSyntaxLine(SyntaxLine newSyntaxLine)
        {
            m_syntaxLines.Add(newSyntaxLine.Name, newSyntaxLine);
        }

        private void Func1(SyntaxLine syntaxLine, int i, ref List<int> indexesHaveLeftCommonFactor, ref List<string> leftCommonFactor)
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
                Func1(syntaxLine, i, ref indexesHaveLeftCommonFactor, ref leftCommonFactor);
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
                    if (production.Symbols.Count == 1 && production.Symbols[0].Equals(Helpers.EmptyOperator.ToString()))
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
            return !m_syntaxLines.Keys.Contains(symbol);
        }

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
                            if ((currentSymbol.Equals(nonTernimalSymbol) && i + 1 == production.Symbols.Count)
                                || (i + 1 < production.Symbols.Count && firstSet[production.Symbols[i + 1]].Contains(Helpers.EmptyOperator.ToString())))
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
    }
}

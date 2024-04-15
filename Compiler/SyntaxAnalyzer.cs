namespace Compiler
{
    public class SyntaxAnalyzer
    {
        private List<Token> m_tokens;
        private List<SyntaxLine> m_syntaxLines;

        public SyntaxAnalyzer(List<Token> tokens, List<SyntaxLine> syntaxLines)
        {
            m_tokens = tokens;
            m_syntaxLines = syntaxLines;
        }

        public void Execute()
        {
            EliminateEmptyProduction();
            EliminateCircle();
            EliminateLeftRecursion();
            ExtractLeftCommonFactor();

            foreach (var syntaxLine in m_syntaxLines)
            {
                Console.WriteLine(syntaxLine.ToString());
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
            // TODO 是否需要对m_syntaxLines排序？理论上是后面的文法表达式中会使用到前面的文法，前面的文法表达式中不使用后面的文法
            List<SyntaxLine> newSyntaxLines = new List<SyntaxLine>();
            for (int i = 0; i < m_syntaxLines.Count; i++)
            {
                Dictionary<Production, List<Production>> productionsToReplace = new Dictionary<Production, List<Production>>();
                for (int j = 0; j < i; j++)
                {
                    // 将每个形如Ai->AjY的产生式替换为产生式组Ai->X1Y|X2Y|...|XkY，
                    // 其中Aj->X1|X2|...|Xk是所有的Aj产生式
                    foreach (var production in m_syntaxLines[i].Productions)
                    {
                        if (production.Symbols[0] == m_syntaxLines[j].Name)
                        {
                            productionsToReplace.Add(production, new List<Production>());
                            List<Production> newProductions2 = m_syntaxLines[i].Productions;
                            foreach (var production2 in m_syntaxLines[j].Productions)
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
                    m_syntaxLines[i].Productions.Remove(toReplace.Key);
                    m_syntaxLines[i].Productions.AddRange(toReplace.Value);
                }
                // 消除新产生式的直接左递归
                SyntaxLine newSyntaxLine = new SyntaxLine();
                newSyntaxLine.Name = string.Format("{0}'", m_syntaxLines[i].Name);
                List<Production> notLeftRecursionProductions = new List<Production>();
                bool haveLeftRecursion = false;
                foreach (var production in m_syntaxLines[i].Productions)
                {
                    if (production.Symbols[0] == m_syntaxLines[i].Name)
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
                    m_syntaxLines[i].Productions = eliminated;
                }
            }
            // 将新产生的文法加入到文法列表中
            foreach (var newSyntaxLine in newSyntaxLines)
                m_syntaxLines.Add(newSyntaxLine);
        }

        /// <summary>
        /// 提取左公因子
        /// </summary>
        private void ExtractLeftCommonFactor()
        {
            List<SyntaxLine> newSyntaxLines = new List<SyntaxLine>();
            foreach (var syntaxLine in m_syntaxLines)
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
                m_syntaxLines.Add(newSyntaxLine);
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
    }
}

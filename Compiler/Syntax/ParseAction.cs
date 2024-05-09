namespace Compiler.Syntax
{
    internal abstract class ParseAction : SyntaxUnit
    {
        public abstract string FunctionName { get; }

        protected List<string> m_arguments = new List<string>();

        public ParseAction(string content) : base(content)
        {
            SyntaxUnitType = SyntaxUnitType.ParseAction;
            CollectArguments(content);
        }

        public abstract void Execute(SyntaxAnalyzer parser, ParserContext parserContext);

        public abstract void RevertExecute(SyntaxAnalyzer parser);

        private void CollectArguments(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            int leftParIndex = content.IndexOf('(');
            if (leftParIndex == -1)
                return;

            var paramsList = content.Substring(leftParIndex, content.Length - 1 - leftParIndex);
            paramsList = paramsList.Trim('(', ')');
            if (paramsList != null)
            {
                var paramsString = paramsList.Split(',');
                foreach (var param in paramsString)
                {
                    if (!string.IsNullOrEmpty(param))
                        m_arguments.Add(param.Trim());
                }
            }
        }
    }
}

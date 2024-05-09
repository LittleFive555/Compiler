namespace Compiler.Syntax
{
    internal class ParseActionFactory
    {
        private static Dictionary<string, Type> m_parseActions;

        public static ParseAction CreateAction(string content)
        {
            CollectParseActions();

            var actionContent = content.Trim('{', '}');
            string actionName;
            if (actionContent.IndexOf('(') == -1)
                actionName = actionContent;
            else
                actionName = actionContent.Substring(0, actionContent.IndexOf('('));

            // NOTE 分辨内容，创建对应的ParseAction
            if (m_parseActions.TryGetValue(actionName, out Type type))
                return (ParseAction)type.Assembly.CreateInstance(type.FullName, false, System.Reflection.BindingFlags.Default, null, new object[] { content }, null, null);
            else
                throw new Exception();
        }

        private static void CollectParseActions()
        {
            if (m_parseActions != null)
                return;

            m_parseActions = new Dictionary<string, Type>();
            var assembly = typeof(ParseAction).Assembly;
            var allTypes = assembly.GetTypes();
            foreach (var type in allTypes)
            {
                if (type.IsSubclassOf(typeof(ParseAction)))
                {
                    var tempObj = (ParseAction)type.Assembly.CreateInstance(type.FullName, false, System.Reflection.BindingFlags.Default, null, new object[] { null }, null, null);
                    m_parseActions.Add(tempObj.FunctionName, type);
                }
            }
        }
    }
}

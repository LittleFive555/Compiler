namespace Compiler.Syntax
{
    internal class ParseActionFactory
    {
        public static ParseAction CreateAction(string content)
        {
            var actionContent = content.Trim('{', '}');
            string actionName;
            if (actionContent.IndexOf('(') == -1)
                actionName = actionContent;
            else
                actionName = actionContent.Substring(0, actionContent.IndexOf('('));

            // TODO 分辨内容，创建对应的ParseAction
            if (actionName == "PushScope")
                return new ParseActionPushScope(content);
            else if (actionName == "PopScope")
                return new ParseActionPopScope(content);
            else if (actionName == "AddSymbolUsage")
                return new ParseActionAddSymbolUsage(content);
            else if (actionName == "AddSymbolDefinition")
                return new ParseActionAddSymbolDefinition(content);
            else
                throw new Exception();
        }
    }
}

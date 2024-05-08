namespace Compiler.Syntax
{
    internal class ParseActionFactory
    {
        public static ParseAction CreateAction(string content)
        {
            var temp = content.Trim('{', '}');
            // TODO 分辨内容，创建对应的ParseAction
            if (temp == "PushScope")
                return new ParseActionPushScope(content);
            else if (temp == "PopScope")
                return new ParseActionPopScope(content);
            else if (temp == "AddSymbolUsage")
                return new ParseActionAddSymbolUsage(content);
            else if (temp == "AddSymbolDefinition")
                return new ParseActionAddSymbolDefinition(content);
            else
                throw new Exception();
        }
    }
}

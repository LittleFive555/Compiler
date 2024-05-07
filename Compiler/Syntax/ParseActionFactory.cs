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
            else if (temp == "AddSymbol")
                return new ParseActionAddSymbol(content);
            else
                throw new Exception();
        }
    }
}

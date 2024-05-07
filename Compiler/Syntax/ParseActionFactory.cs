namespace Compiler.Syntax
{
    public class ParseActionFactory
    {
        public static ParseAction CreateAction(string content)
        {
            content = content.Trim('{', '}');
            // TODO 分辨内容，创建对应的ParseAction
            return new ParseActionPrint(content);
        }
    }
}

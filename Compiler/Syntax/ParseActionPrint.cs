namespace Compiler.Syntax
{
    public class ParseActionPrint : ParseAction
    {
        public ParseActionPrint(string content) : base(content)
        {
        }

        public override void Execute()
        {
            Console.WriteLine("Execute Print.");
        }
    }
}

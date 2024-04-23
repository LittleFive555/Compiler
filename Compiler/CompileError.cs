namespace Compiler
{
    public class CompileError
    {
        public int Line { get; }
        public int Column { get; }
        public int Length { get; }
        public string Info { get; }

        public CompileError(int line, int column, int length, string info)
        {
            Line = line;
            Column = column;
            Length = length;
            Info = info;
        }

        public override string ToString()
        {
            return string.Format("Error (Line:{0}, Column:{1}):{2}", Line, Column, Info);
        }
    }
}

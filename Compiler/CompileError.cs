namespace Compiler
{
    public class CompileError
    {
        public string File { get; }
        public int Line { get; }
        public int Column { get; }
        public int Length { get; }
        public string Info { get; }

        public CompileError(string file, int line, int column, int length, string info)
        {
            File = file;
            Line = line;
            Column = column;
            Length = length;
            Info = info;
        }

        public override string ToString()
        {
            return string.Format("Error [{0}](Line:{1}, Column:{2}):{3}", File, Line, Column, Info);
        }
    }
}

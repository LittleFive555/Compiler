﻿namespace Compiler.Lexical
{
    public class Token : IEquatable<Token?>
    {
        public string Content { get; }
        public LexicalUnit LexicalUnit { get; }
        public FileData FileData { get; }
        public int Line { get; }
        public int StartColumn { get; }
        public int Length { get; }

        public Token(string content, LexicalUnit lexicalUnit, FileData fileData, int line, int startColumn, int length)
        {
            Content = content;
            LexicalUnit = lexicalUnit;
            FileData = fileData;
            Line = line;
            StartColumn = startColumn;
            Length = length;
        }

        public override string ToString()
        {
            return string.Format("Content: {0}, LexicalUnitName: {1}, Line: {2}, Column:{3}, Length: {4}", Content, LexicalUnit.Name, Line, StartColumn, Length);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Token);
        }

        public bool Equals(Token? other)
        {
            return other is not null &&
                   Content == other.Content &&
                   FileData == other.FileData &&
                   Line == other.Line &&
                   StartColumn == other.StartColumn &&
                   Length == other.Length;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Content, FileData, Line, StartColumn, Length);
        }

        public static bool operator ==(Token? left, Token? right)
        {
            return EqualityComparer<Token>.Default.Equals(left, right);
        }

        public static bool operator !=(Token? left, Token? right)
        {
            return !(left == right);
        }
    }
}

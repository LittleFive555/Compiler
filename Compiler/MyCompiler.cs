using Compiler.Lexical;
using Compiler.Syntax;

namespace Compiler
{
    public class MyCompiler
    {
        private LexicalAnalyzer m_lexicalAnalyzer;

        private SyntaxAnalyzer m_syntaxAnalyzer;

        public MyCompiler(string symbolDefineFile, string reservedWordDefineFile, string lexicalDefineFile, string syntaxDefineFile)
        {
            // XXX 需要先读取自定义的词法正则表达式，再读取保留字和特殊符号，因为有判断优先级的问题
            List<LexicalRegex> allLexicalRegex =
            [
                new LexicalRegex(Helpers.WhitespaceName, Helpers.WhitespaceRegex, LexicalType.Whitespace, -1),
                .. LexicalRegexLoader.ReadRegexFromFile(LexicalType.Other, lexicalDefineFile),
                .. LexicalRegexLoader.ReadRegexFromFile(LexicalType.Symbol, symbolDefineFile),
                .. LexicalRegexLoader.ReadRegexFromFile(LexicalType.ReservedWord, reservedWordDefineFile),
            ];

            m_lexicalAnalyzer = new LexicalAnalyzer(allLexicalRegex.ToArray());

            var syntaxLines = SyntaxReader.ReadFromFile(syntaxDefineFile);
            m_syntaxAnalyzer = new SyntaxAnalyzer(syntaxLines);
        }

        public void Analyze(string filePath)
        {
            Analyze(File.OpenRead(filePath));
        }

        public void Analyze(Stream stream)
        {
            var result = m_lexicalAnalyzer.Read(stream);

            foreach (var token in result.Tokens)
                Console.WriteLine(token);
            foreach (var error in result.Errors)
                Console.WriteLine(error);

            if (result.Errors.Count > 0)
                return;

            m_syntaxAnalyzer.Execute(result.Tokens);
        }
    }
}

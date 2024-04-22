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
            List<LexicalRegex> allLexicalRegex = new List<LexicalRegex>();
            allLexicalRegex.Add(new LexicalRegex(Helpers.WhitespaceName, Helpers.WhitespaceRegex, LexicalType.Whitespace, -1));
            allLexicalRegex.AddRange(LexicalRegexLoader.ReadRegexFromFile(LexicalType.Other, lexicalDefineFile));
            allLexicalRegex.AddRange(LexicalRegexLoader.ReadRegexFromFile(LexicalType.Symbol, symbolDefineFile));
            allLexicalRegex.AddRange(LexicalRegexLoader.ReadRegexFromFile(LexicalType.ReservedWord, reservedWordDefineFile));
            allLexicalRegex.Add(new LexicalRegex(Helpers.SingleLineCommentName, Helpers.SingleLineCommentRegex, LexicalType.Comment, 1000));
            allLexicalRegex.Add(new LexicalRegex(Helpers.BlockCommentLeftName, Helpers.BlockCommentLeftRegex, LexicalType.Comment, 1001));
            allLexicalRegex.Add(new LexicalRegex(Helpers.BlockCommentRightName, Helpers.BlockCommentRightRegex, LexicalType.Comment, 1002));
            m_lexicalAnalyzer = new LexicalAnalyzer(allLexicalRegex.ToArray());

            var syntaxLines = SyntaxReader.ReadFromFile(syntaxDefineFile);
            m_syntaxAnalyzer = new SyntaxAnalyzer(syntaxLines);
        }

        public AnalyzeResult Analyze(string filePath)
        {
            return Analyze(File.OpenRead(filePath));
        }

        public AnalyzeResult Analyze(Stream stream)
        {
            AnalyzeResult analyzeResult = new AnalyzeResult();
            var lexicalResult = m_lexicalAnalyzer.Read(stream);

            MyLogger.WriteLine("Tokens:");
            foreach (var token in lexicalResult.Tokens)
                MyLogger.WriteLine(token);

            analyzeResult.CompileErrors.AddRange(lexicalResult.Errors);

            if (lexicalResult.Errors.Count > 0)
                return analyzeResult;

            var syntaxResult = m_syntaxAnalyzer.Execute(lexicalResult.Tokens);
            analyzeResult.CompileErrors.AddRange(syntaxResult.Errors);

            return analyzeResult;
        }
    }

    public class AnalyzeResult
    {
        public List<CompileError> CompileErrors = new List<CompileError>();
    }
}

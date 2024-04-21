internal static class Helpers
{
    public const char EmptyOperator = '·';

    public const string WhitespaceName = "Whitespace";
    public const string WhitespaceRegex = "( |\t|\r|\n)+";

    public const string SingleLineCommentName = "SingleLineComment";
    public const string SingleLineCommentRegex = "//";

    public const string BlockCommentName = "BlockComment";
    public const string BlockCommentLeftName = "BlockCommentLeft";
    public const string BlockCommentLeftRegex = "/\\*";
    public const string BlockCommentRightName = "BlockCommentRight";
    public const string BlockCommentRightRegex = "\\*/";
}
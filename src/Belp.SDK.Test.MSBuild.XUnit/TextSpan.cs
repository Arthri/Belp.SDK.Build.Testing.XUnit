using Microsoft.Build.Framework;

namespace Belp.SDK.Test.MSBuild.XUnit;

internal record struct TextSpan(TextPosition Start, TextPosition End)
{
    public TextSpan(BuildMessageEventArgs e)
        : this(new(e.LineNumber, e.ColumnNumber), new(e.EndLineNumber, e.EndColumnNumber))
    {
    }

    public TextSpan(BuildWarningEventArgs e)
        : this(new(e.LineNumber, e.ColumnNumber), new(e.EndLineNumber, e.EndColumnNumber))
    {
    }

    public TextSpan(BuildErrorEventArgs e)
        : this(new(e.LineNumber, e.ColumnNumber), new(e.EndLineNumber, e.EndColumnNumber))
    {
    }

    public override readonly string ToString()
    {
        return $"{Start.Line}:{Start.Column},${End.Line}:${End.Column}";
    }
}

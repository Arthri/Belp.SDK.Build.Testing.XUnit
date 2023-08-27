using Microsoft.Build.Framework;
using System;

namespace Belp.SDK.Test.MSBuild.XUnit;

internal record struct Diagnostic(DiagnosticSeverity Severity, string Code, string? Message, string File, TextSpan Span, string Project)
{
    public Diagnostic(BuildMessageEventArgs e)
        : this(
            e.Importance switch
            {
                MessageImportance.High => DiagnosticSeverity.Informational,
                MessageImportance.Normal => DiagnosticSeverity.Verbose,
                MessageImportance.Low => DiagnosticSeverity.Diagnostic,
                _ => throw new NotSupportedException(),
            },
            e.Code,
            e.Message,
            e.File,
            new TextSpan(e),
            e.ProjectFile
        )
    {
    }

    public Diagnostic(BuildWarningEventArgs e)
        : this(DiagnosticSeverity.Warning, e.Code, e.Message, e.File, new TextSpan(e), e.ProjectFile)
    {
    }

    public Diagnostic(BuildErrorEventArgs e)
        : this(DiagnosticSeverity.Error, e.Code, e.Message, e.File, new TextSpan(e), e.ProjectFile)
    {
    }

    public override readonly string ToString()
    {
        string levelAbbr = Severity switch
        {
            DiagnosticSeverity.Critical => "CRT",
            DiagnosticSeverity.Error => "ERR",
            DiagnosticSeverity.Warning => "WRN",
            DiagnosticSeverity.Informational => "INF",
            DiagnosticSeverity.Verbose => "VRB",
            DiagnosticSeverity.Diagnostic => "DBG",
            _ => throw new NotSupportedException(),
        };
        return $"[{levelAbbr}] {Code}{(Message is null ? "" : $": {Message}")} @ {File}({Span}) [{Project}]";
    }
}

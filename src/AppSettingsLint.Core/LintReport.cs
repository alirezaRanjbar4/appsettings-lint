namespace AppSettingsLint.Core;

public sealed record SourceLocation(int Line, int Column);

public sealed record Diagnostic(string Code, string Severity, string File, string? Key, string Message,
    SourceLocation? Location = null, SourceLocation? OriginalLocation = null);

public sealed record LintReport(int FilesChecked, int KeysChecked, IReadOnlyList<Diagnostic> Diagnostics)
{
    public int Errors => Diagnostics.Count(d => d.Severity == "error");
    public int Warnings => Diagnostics.Count(d => d.Severity == "warning");
}

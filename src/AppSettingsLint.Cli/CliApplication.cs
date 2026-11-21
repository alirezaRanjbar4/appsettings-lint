using System.Text.Json;
using AppSettingsLint.Core;

namespace AppSettingsLint.Cli;

public static class CliApplication
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var files = new List<string>();
        var required = new List<string>();
        var strict = false;
        var format = "text";
        var positionalOnly = false;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (positionalOnly)
            {
                files.Add(argument);
                continue;
            }
            switch (argument)
            {
                case "--help" or "-h":
                    output.WriteLine(Help);
                    return 0;
                case "--version":
                    output.WriteLine("0.1.1");
                    return 0;
                case "--":
                    positionalOnly = true;
                    break;
                case "--strict":
                    strict = true;
                    break;
                case "--require" or "--format":
                    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                        return UsageError($"Missing value for {argument}.", error);
                    var value = args[++index];
                    if (argument == "--require")
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            return UsageError("A required key cannot be blank.", error);
                        required.Add(value);
                    }
                    else
                    {
                        if (value is not ("json" or "text"))
                            return UsageError("Format must be text or json.", error);
                        format = value;
                    }
                    break;
                default:
                    if (argument.StartsWith('-'))
                        return UsageError("Unknown option. Use --help for usage.", error);
                    files.Add(argument);
                    break;
            }
        }
        if (files.Count == 0)
            return UsageError("Pass at least one JSON file. Use --help for usage.", error);
        var report = ConfigurationLinter.Lint(files, required);
        if (format == "json")
            output.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
        else
        {
            foreach (var diagnostic in report.Diagnostics)
                output.WriteLine($"{diagnostic.File}: {diagnostic.Severity} {diagnostic.Code}" +
                    (diagnostic.Key is null ? "" : $" [{diagnostic.Key}]") + $" {diagnostic.Message}");
            output.WriteLine($"Checked {report.FilesChecked} file(s), {report.KeysChecked} effective key(s): {report.Errors} error(s), {report.Warnings} warning(s).");
        }
        return report.Errors > 0 || (strict && report.Warnings > 0) ? 1 : 0;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static int UsageError(string message, TextWriter error)
    {
        error.WriteLine(message);
        return 2;
    }

    private const string Help = """
        appsettings-lint — check .NET JSON settings before deployment

        Usage: appsettings-lint <file.json> [overlay.json ...] [options]

        Files are layered left to right. Later files override earlier values.
        --require <Section:Key>  Require a nonblank leaf value; repeat for more keys.
        --strict                 Return failure for warnings as well as errors.
        --format <text|json>     Select output format (default: text).
        --version                Print the tool version.
        --help, -h               Show this help.
        --                       Treat all remaining arguments as file paths.

        Exit codes: 0 = passed, 1 = lint findings, 2 = invalid arguments.
        Values are never included in reports. No environment variables, user
        secrets, remote services, or application code are loaded.
        """;
}

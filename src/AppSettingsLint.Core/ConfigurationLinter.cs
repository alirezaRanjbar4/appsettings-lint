using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppSettingsLint.Core;

public static partial class ConfigurationLinter
{
    private sealed record Setting(string? Value, string File);

    public static LintReport Lint(IEnumerable<string> files, IEnumerable<string>? requiredKeys = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        var diagnostics = new List<Diagnostic>();
        var effective = new Dictionary<string, Setting>(StringComparer.OrdinalIgnoreCase);
        var filesChecked = 0;
        foreach (var file in files)
        {
            filesChecked++;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    diagnostics.Add(new("ASL001", "error", file, null, "The root must be a JSON object."));
                    continue;
                }
                var current = new Dictionary<string, Setting>(StringComparer.OrdinalIgnoreCase);
                Visit(document.RootElement, null, file, current, diagnostics);
                foreach (var (key, value) in current)
                    effective[key] = value;
            }
            catch (JsonException exception)
            {
                // JsonException.Message can contain source text, including credentials.
                diagnostics.Add(new("ASL001", "error", file, null,
                    $"Invalid JSON at line {(exception.LineNumber ?? 0) + 1}, byte {(exception.BytePositionInLine ?? 0) + 1}."));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                diagnostics.Add(new("ASL003", "error", file, null, "The file could not be read."));
            }
        }
        foreach (var key in (requiredKeys ?? []).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!effective.TryGetValue(key, out var setting))
                diagnostics.Add(new("ASL004", "error", "(effective configuration)", key, "Required setting is missing. Use a leaf key, such as Database:Host."));
            else if (string.IsNullOrWhiteSpace(setting.Value))
                diagnostics.Add(new("ASL005", "error", setting.File, key, "Required setting is null or blank."));
        }
        foreach (var (key, setting) in effective.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (setting.Value is { } value &&
                (PlaceholderPattern().IsMatch(value) || value.Trim().Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase)))
                diagnostics.Add(new("ASL006", "warning", setting.File, key, "Unresolved placeholder in the effective value."));
        }
        return new(filesChecked, effective.Count, diagnostics);
    }

    private static void Visit(JsonElement element, string? path, string file,
        Dictionary<string, Setting> settings, List<Diagnostic> diagnostics)
    {
        if (element.ValueKind == JsonValueKind.Object && element.EnumerateObject().Any())
        {
            foreach (var property in element.EnumerateObject())
                Visit(property.Value, path is null ? property.Name : $"{path}:{property.Name}", file, settings, diagnostics);
            return;
        }
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
                Visit(item, $"{path}:{index++}", file, settings, diagnostics);
            return;
        }
        if (path is null)
            return;
        var value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Object or JsonValueKind.Array => null,
            _ => element.GetRawText()
        };
        if (!settings.TryAdd(path, new(value, file)))
            diagnostics.Add(new("ASL002", "error", file, path, "Duplicate configuration key (case-insensitive, flattened path)."));
    }

    [GeneratedRegex(@"\$\{[^{}]+\}|#\{[^{}]+\}#|\{\{[^{}]+\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}

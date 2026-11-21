using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace AppSettingsLint.Core;

public static partial class ConfigurationLinter
{
    private sealed record Setting(string? Value, string File, SourceLocation Location);

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
                var text = File.ReadAllText(file);
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions
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
                var source = Encoding.UTF8.GetBytes(text);
                var reader = new Utf8JsonReader(source, new JsonReaderOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                reader.Read();
                var coordinates = new SourceCoordinates(source);
                Visit(ref reader, null, coordinates.At(reader.TokenStartIndex), coordinates, file, current, diagnostics);
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
                diagnostics.Add(new("ASL005", "error", setting.File, key, "Required setting is null or blank.", setting.Location));
        }
        foreach (var (key, setting) in effective.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (setting.Value is { } value &&
                (PlaceholderPattern().IsMatch(value) || value.Trim().Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase)))
                diagnostics.Add(new("ASL006", "warning", setting.File, key, "Unresolved placeholder in the effective value.", setting.Location));
        }
        return new(filesChecked, effective.Count, diagnostics);
    }

    private static void Visit(ref Utf8JsonReader reader, string? path, SourceLocation location, SourceCoordinates coordinates, string file,
        Dictionary<string, Setting> settings, List<Diagnostic> diagnostics)
    {
        string? value = null;
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var hasChildren = false;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                hasChildren = true;
                var name = reader.GetString()!;
                var propertyLocation = coordinates.At(reader.TokenStartIndex);
                reader.Read();
                Visit(ref reader, path is null ? name : $"{path}:{name}", propertyLocation, coordinates, file, settings, diagnostics);
            }
            if (hasChildren) return;
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            var index = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var itemLocation = coordinates.At(reader.TokenStartIndex);
                Visit(ref reader, $"{path}:{index++}", itemLocation, coordinates, file, settings, diagnostics);
            }
            if (index > 0) return;
        }
        else
            value = reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Null => null,
                _ => Encoding.UTF8.GetString(reader.ValueSpan)
            };

        if (path is null)
            return;
        if (!settings.TryAdd(path, new(value, file, location)))
            diagnostics.Add(new("ASL002", "error", file, path, "Duplicate configuration key (case-insensitive, flattened path).", location, settings[path].Location));
    }

    [GeneratedRegex(@"\$\{[^{}]+\}|#\{[^{}]+\}#|\{\{[^{}]+\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}

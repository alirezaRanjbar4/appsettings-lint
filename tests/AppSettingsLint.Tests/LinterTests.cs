using System.Text.Json;
using AppSettingsLint.Cli;
using AppSettingsLint.Core;

namespace AppSettingsLint.Tests;

public sealed class LinterTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "appsettings-lint-tests-" + Guid.NewGuid().ToString("N"));

    private string Write(string json)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Theory]
    [InlineData("{\"Api\":1,\"api\":2}", "api")]
    [InlineData("{\"Api\":{\"Url\":1},\"Api:Url\":2}", "Api:Url")]
    [InlineData("{\"Nodes\":[\"one\"],\"Nodes:0\":\"two\"}", "Nodes:0")]
    [InlineData("{\"Api\":{},\"api\":null}", "api")]
    [InlineData("{\"\":{\"Key\":1},\":Key\":2}", ":Key")]
    public void DetectsDuplicateFlattenedKeys(string json, string key)
    {
        var report = ConfigurationLinter.Lint([Write(json)]);
        var diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal("ASL002", diagnostic.Code);
        Assert.Equal(key, diagnostic.Key, ignoreCase: true);
    }

    [Fact]
    public void AcceptsCommentsTrailingCommasAndArrays()
    {
        var file = Write("""
            { // supported by .NET JSON configuration
              "Servers": [{"Host":"localhost"},],
              "Enabled": false,
              "Port": 0,
            }
            """);
        var report = ConfigurationLinter.Lint([file], ["servers:0:host", "Enabled", "Port"]);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(3, report.KeysChecked);
    }

    [Fact]
    public void LaterFileOverridesCaseInsensitivelyBeforePlaceholderChecks()
    {
        var first = Write("{\"Api\":{\"Token\":\"${TOKEN}\",\"Host\":\"localhost\"}}");
        var second = Write("{\"api\":{\"token\":\"synthetic-test-value\"}}");
        var report = ConfigurationLinter.Lint([first, second], ["API:TOKEN", "Api:Host"]);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(2, report.FilesChecked);
        Assert.Equal(2, report.KeysChecked);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void RequiredKeyMustHaveANonblankLeafValue(string value)
    {
        var report = ConfigurationLinter.Lint([Write("{\"Required\":" + value + "}")], ["Required"]);
        Assert.Equal("ASL005", Assert.Single(report.Diagnostics).Code);
    }

    [Fact]
    public void MissingRequirementIsReportedOnceRegardlessOfCasing()
    {
        var report = ConfigurationLinter.Lint([Write("{}")], ["Missing", "MISSING"]);
        Assert.Equal("ASL004", Assert.Single(report.Diagnostics).Code);
    }

    [Theory]
    [InlineData("${TOKEN}")]
    [InlineData("Server=#{HOST}#;")]
    [InlineData("{{TOKEN}}")]
    [InlineData("change_me")]
    public void DetectsUnresolvedPlaceholdersWithoutPrintingValues(string value)
    {
        var report = ConfigurationLinter.Lint([Write(JsonSerializer.Serialize(new { Token = value }))]);
        Assert.Equal("ASL006", Assert.Single(report.Diagnostics).Code);
        Assert.DoesNotContain(value, JsonSerializer.Serialize(report));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"string\"")]
    [InlineData("42")]
    [InlineData("{\"Token\":synthetic-secret-value}")]
    public void InvalidJsonAndNonObjectRootsProduceSafeDiagnostics(string json)
    {
        var report = ConfigurationLinter.Lint([Write(json)]);
        Assert.Equal("ASL001", Assert.Single(report.Diagnostics).Code);
        Assert.DoesNotContain("synthetic-secret-value", JsonSerializer.Serialize(report));
    }

    [Fact]
    public void MissingFileDoesNotPreventCheckingRemainingFiles()
    {
        var report = ConfigurationLinter.Lint([Path.Combine(directory, "missing.json"), Write("{\"Good\":1}")]);
        Assert.Equal("ASL003", Assert.Single(report.Diagnostics).Code);
        Assert.Equal(1, report.KeysChecked);
    }

    [Fact]
    public void ShorterArrayOverlayRetainsEarlierHigherIndexes()
    {
        var report = ConfigurationLinter.Lint([Write("{\"Servers\":[\"a\",\"b\"]}"), Write("{\"Servers\":[\"c\"]}")], ["Servers:1"]);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(2, report.KeysChecked);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void StrictModeControlsWarningExitCode(bool strict, int expected)
    {
        var args = new List<string> { Write("{\"Token\":\"${TOKEN}\"}") };
        if (strict) args.Add("--strict");
        Assert.Equal(expected, CliApplication.Run(args.ToArray(), new StringWriter(), new StringWriter()));
    }

    [Fact]
    public void JsonOutputIsParseableAndContainsNoValues()
    {
        var output = new StringWriter();
        var exit = CliApplication.Run([Write("{\"Token\":\"synthetic-private-value\"}"), "--format", "json"], output, new StringWriter());
        Assert.Equal(0, exit);
        using var report = JsonDocument.Parse(output.ToString());
        Assert.Equal(1, report.RootElement.GetProperty("keysChecked").GetInt32());
        Assert.DoesNotContain("synthetic-private-value", output.ToString());
    }

    public static TheoryData<string[]> InvalidArguments => new()
    {
        Array.Empty<string>(),
        new[] { "--require" },
        new[] { "--require", "--strict" },
        new[] { "--require", " " },
        new[] { "--format", "yaml" },
        new[] { "--unknown" }
    };

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void InvalidArgumentsReturnUsageExitCode(string[] args)
    {
        var error = new StringWriter();
        Assert.Equal(2, CliApplication.Run(args, new StringWriter(), error));
        Assert.NotEmpty(error.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    public void HelpAndVersionDoNotRequireFiles(string argument)
    {
        var output = new StringWriter();
        Assert.Equal(0, CliApplication.Run([argument], output, new StringWriter()));
        Assert.NotEmpty(output.ToString());
    }

    public void Dispose()
    {
        // Every file is created in this test's unique, absolute temporary directory.
        if (Directory.Exists(directory) && Path.GetFileName(directory).StartsWith("appsettings-lint-tests-", StringComparison.Ordinal))
            Directory.Delete(directory, recursive: true);
    }
}

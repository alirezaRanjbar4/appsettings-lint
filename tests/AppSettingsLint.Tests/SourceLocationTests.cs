using System.Text;
using System.Text.Json;
using AppSettingsLint.Cli;
using AppSettingsLint.Core;

namespace AppSettingsLint.Tests;

public sealed class SourceLocationTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "appsettings-location-" + Guid.NewGuid().ToString("N"));
    private string Write(string json, Encoding? encoding = null)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json, encoding ?? new UTF8Encoding(false));
        return path;
    }

    [Fact]
    public void CaseOnlyDuplicateReportsBothPropertyStarts()
    {
        var report = ConfigurationLinter.Lint([Write("{\n  \"Port\": 80,\n  \"port\": 81\n}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal("ASL002", finding.Code);
        Assert.Equal(new SourceLocation(3, 3), finding.Location);
        Assert.Equal(new SourceLocation(2, 3), finding.OriginalLocation);
    }

    [Fact]
    public void NestedAndColonPathsHaveTheirOwnSourcePositions()
    {
        var report = ConfigurationLinter.Lint([Write("{\n  \"Api\": { \"Url\": \"one\" },\n  \"Api:Url\": \"two\"\n}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal("Api:Url", finding.Key);
        Assert.Equal(new SourceLocation(2, 12), finding.OriginalLocation);
        Assert.Equal(new SourceLocation(3, 3), finding.Location);
    }

    [Fact]
    public void EscapedPropertyNamesAreDecodedForIdentityButKeepSourceOffsets()
    {
        var report = ConfigurationLinter.Lint([Write("{\n  \"\\u0041pi\": 1,\n  \"api\": 2\n}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal("api", finding.Key);
        Assert.Equal(new SourceLocation(2, 3), finding.OriginalLocation);
        Assert.Equal(new SourceLocation(3, 3), finding.Location);
    }

    [Fact]
    public void ColumnsCountUnicodeScalarsNotUtf8BytesOrUtf16Surrogates()
    {
        var report = ConfigurationLinter.Lint([Write("{\"ی\":\"😀\",\"Token\":\"${SECRET}\"}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal("ASL006", finding.Code);
        Assert.Equal(new SourceLocation(1, 10), finding.Location);
        Assert.Null(finding.OriginalLocation);
    }

    [Fact]
    public void BomCrLfAndTabsDoNotShiftTheFirstProperty()
    {
        var report = ConfigurationLinter.Lint([Write("{\r\n\t\"Token\": \"${TOKEN}\"\r\n}", new UTF8Encoding(true))]);
        Assert.Equal(new SourceLocation(2, 2), Assert.Single(report.Diagnostics).Location);
    }

    [Fact]
    public void ArraysCommentsTrailingCommasAndFlattenedCollisionsKeepLocations()
    {
        var report = ConfigurationLinter.Lint([Write("{\n // note\n \"Nodes\": [\n  \"one\",\n ],\n \"nodes:0\": \"two\",\n}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal(new SourceLocation(4, 3), finding.OriginalLocation);
        Assert.Equal(new SourceLocation(6, 2), finding.Location);
    }

    [Fact]
    public void ArrayPlaceholdersPointAtTheIndexedValueToken()
    {
        var report = ConfigurationLinter.Lint([Write("{\n \"Nodes\": [\n  \"${TOKEN}\"\n ]\n}")]);
        var finding = Assert.Single(report.Diagnostics);
        Assert.Equal("Nodes:0", finding.Key);
        Assert.Equal(new SourceLocation(3, 3), finding.Location);
    }

    [Fact]
    public void OverlayWarningUsesTheWinningFileAndLocationOnly()
    {
        var first = Write("{\"Token\":\"${BASE}\"}");
        var last = Write("{\n  \"Other\": \"safe\",\n  \"token\": \"${CANDIDATE}\"\n}");
        var finding = Assert.Single(ConfigurationLinter.Lint([first, last]).Diagnostics);
        Assert.Equal(last, finding.File);
        Assert.Equal(new SourceLocation(3, 3), finding.Location);
        Assert.Null(finding.OriginalLocation);
        Assert.Empty(ConfigurationLinter.Lint([last, Write("{\"Token\":\"safe\"}")]).Diagnostics);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public void EmptyContainerDuplicatesPointAtPropertyNames(string container)
    {
        var finding = Assert.Single(ConfigurationLinter.Lint([Write("{\n \"A\":" + container + ",\n \"a\":" + container + "\n}")]).Diagnostics);
        Assert.Equal(new SourceLocation(2, 2), finding.OriginalLocation);
        Assert.Equal(new SourceLocation(3, 2), finding.Location);
    }

    [Fact]
    public void EmptyParentNamesDoNotLoseTheOriginalNestedKeyPosition()
    {
        var finding = Assert.Single(ConfigurationLinter.Lint([Write("{\n \"\": {\"Key\": 1},\n \":Key\": 2\n}")]).Diagnostics);
        Assert.Equal(":Key", finding.Key);
        Assert.Equal(new SourceLocation(2, 7), finding.OriginalLocation);
        Assert.Equal(new SourceLocation(3, 2), finding.Location);
    }

    [Fact]
    public void JsonAndTextIncludeCoordinatesWithoutValuesOrSnippets()
    {
        var path = Write("{\n \"Token\":\"synthetic-private-one\",\n \"token\":\"synthetic-private-two\"\n}");
        var output = new StringWriter();
        Assert.Equal(1, CliApplication.Run([path, "--format", "json"], output, new StringWriter()));
        using var json = JsonDocument.Parse(output.ToString());
        var finding = json.RootElement.GetProperty("diagnostics")[0];
        Assert.Equal(3, finding.GetProperty("location").GetProperty("line").GetInt32());
        Assert.Equal(2, finding.GetProperty("originalLocation").GetProperty("line").GetInt32());
        Assert.DoesNotContain("synthetic-private", output.ToString());
        output = new StringWriter();
        Assert.Equal(1, CliApplication.Run([path], output, new StringWriter()));
        Assert.Contains("(3,2): error ASL002", output.ToString());
        Assert.Contains("Original occurrence: (2,2).", output.ToString());
        Assert.DoesNotContain("synthetic-private", output.ToString());
    }

    [Fact]
    public void BlankRequiredValuesAlsoIdentifyTheirSource()
    {
        var report = ConfigurationLinter.Lint([Write("{\n \"Required\":null\n}")], ["Required"]);
        Assert.Equal(new SourceLocation(2, 2), Assert.Single(report.Diagnostics).Location);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory) && Path.GetFileName(directory).StartsWith("appsettings-location-", StringComparison.Ordinal)) Directory.Delete(directory, true);
    }
}

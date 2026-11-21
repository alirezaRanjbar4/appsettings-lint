using System.Text;
using AppSettingsLint.Core;
using Microsoft.Extensions.Configuration;

namespace AppSettingsLint.Tests;

public sealed class ProviderParityTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "appsettings-parity-" + Guid.NewGuid().ToString("N"));
    private static ConfigurationRoot Build(params string[] documents)
    {
        var builder = new ConfigurationBuilder();
        foreach (var json in documents) builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        return (ConfigurationRoot)builder.Build();
    }
    private string Write(string json)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Theory]
    [InlineData("{ /* comment */ \"Api\":{\"Port\":0,\"Enabled\":false,},}", "Api:Port", "Api:Enabled")]
    [InlineData("{\"Api:Host\":\"localhost\",\"Servers\":[{\"Host\":\"one\"}]}", "Api:Host", "Servers:0:Host")]
    [InlineData("{\"\":{\"Key\":\"value\"},\"Other\":\"x\"}", ":Key", "Other")]
    public void AcceptsTheSameFlattenedLeafKeys(string json, string firstKey, string secondKey)
    {
        using var config = Build(json);
        Assert.All(new[] { firstKey, secondKey }, key => Assert.True(config.Providers.Single().TryGet(key, out _)));
        var report = ConfigurationLinter.Lint([Write(json)], [firstKey.ToUpperInvariant(), secondKey]);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(2, report.KeysChecked);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\"\"")]
    public void EmptyValuesArePresentButDoNotSatisfyRequiredValues(string value)
    {
        var json = "{\"Required\":" + value + "}";
        using var config = Build(json);
        Assert.True(config.Providers.Single().TryGet("Required", out var actual));
        Assert.True(string.IsNullOrWhiteSpace(actual));
        var report = ConfigurationLinter.Lint([Write(json)], ["Required"]);
        Assert.Equal("ASL005", Assert.Single(report.Diagnostics).Code);
        Assert.Equal(1, report.KeysChecked);
    }

    [Theory]
    [InlineData("{\"Key\":1,\"key\":2}")]
    [InlineData("{\"Api\":{\"Host\":\"one\"},\"Api:Host\":\"two\"}")]
    [InlineData("{\"Servers\":[\"one\"],\"Servers:0\":\"two\"}")]
    public void BothRejectCollidingScalarKeys(string json)
    {
        Assert.Throws<FormatException>(() => new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build());
        Assert.Contains(ConfigurationLinter.Lint([Write(json)]).Diagnostics, diagnostic => diagnostic.Code == "ASL002");
    }

    [Fact]
    public void EmptyOverlayKeepsHigherArrayIndexesAndExistingChildren()
    {
        const string first = """{"Items":["one","two"],"Section":{"Child":"present"},"Token":"${TOKEN}"}""";
        const string second = """{"Items":["three"],"Section":null,"token":"synthetic-value"}""";
        using var config = Build(first, second);
        Assert.Equal("two", config["Items:1"]);
        Assert.Equal("present", config["Section:Child"]);
        Assert.Null(config["Section"]);
        Assert.Equal("synthetic-value", config["TOKEN"]);
        var report = ConfigurationLinter.Lint([Write(first), Write(second)], ["Items:1", "Section:Child", "Token"]);
        Assert.Empty(report.Diagnostics);
        Assert.Equal(5, report.KeysChecked);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    public void LinterIntentionallyRejectsRepeatedEmptyContainersMoreStrictly(string container)
    {
        var json = "{\"Section\":" + container + ",\"section\":" + container + "}";
        using var config = Build(json);
        Assert.True(config.Providers.Single().TryGet("Section", out var value));
        Assert.Equal(container == "[]" ? "" : null, value);
        Assert.Equal("ASL002", Assert.Single(ConfigurationLinter.Lint([Write(json)]).Diagnostics).Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory) && Path.GetFileName(directory).StartsWith("appsettings-parity-", StringComparison.Ordinal)) Directory.Delete(directory, true);
    }
}

# appsettings-lint

[![CI](https://github.com/alirezaRanjbar4/appsettings-lint/actions/workflows/ci.yml/badge.svg)](https://github.com/alirezaRanjbar4/appsettings-lint/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Catch configuration mistakes **before deploying a .NET application**.

`appsettings-lint` is a small, local CLI that checks JSON configuration for duplicate keys, missing required values, and unresolved placeholders. Reports contain file paths and key names, **never configuration values**. The tool has no runtime package dependencies and does not send files anywhere.

## Try it in two minutes

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows, Linux, or macOS.

```sh
git clone https://github.com/alirezaRanjbar4/appsettings-lint.git
cd appsettings-lint
dotnet run --project src/AppSettingsLint.Cli -- examples/invalid.json --require Api:BaseUrl --strict
```

Expected output (exit code `1`):

```text
examples/invalid.json(3,56): error ASL002 [Logging:LogLevel:default] Duplicate configuration key (case-insensitive, flattened path). Original occurrence: (3,30).
examples/invalid.json(2,12): error ASL005 [Api:BaseUrl] Required setting is null or blank.
examples/invalid.json(2,27): warning ASL006 [Api:Token] Unresolved placeholder in the effective value.
Checked 1 file(s), 3 effective key(s): 2 error(s), 1 warning(s).
```

Now check the base file with a production overlay:

```sh
dotnet run --project src/AppSettingsLint.Cli -- examples/appsettings.json examples/appsettings.Production.json --require Api:Token --strict
```

This passes: the production file replaces the base `${API_TOKEN}` placeholder. The sample production value is synthetic, not a credential.

## Install as a .NET tool

Download `AppSettingsLint.Tool.0.2.0.nupkg` from the [v0.2.0 release](https://github.com/alirezaRanjbar4/appsettings-lint/releases/tag/v0.2.0) into an empty folder, then run:

```sh
dotnet tool install --global AppSettingsLint.Tool --version 0.2.0 --add-source ./downloaded-package
appsettings-lint appsettings.json appsettings.Production.json --require ConnectionStrings:Main --strict
```

Or build the package from source:

```sh
dotnet pack src/AppSettingsLint.Cli -c Release -o ./artifacts
dotnet tool install --global AppSettingsLint.Tool --version 0.2.0 --add-source ./artifacts
```

The package is distributed through GitHub Releases. It is **not currently published on NuGet.org**.

## Checks

| Code | Severity | Meaning |
| --- | --- | --- |
| ASL001 | Error | Invalid JSON or a root that is not an object |
| ASL002 | Error | Duplicate case-insensitive, flattened key within one file |
| ASL003 | Error | File cannot be read |
| ASL004 | Error | Required leaf key is missing after layering |
| ASL005 | Error | Required value is null, empty, or whitespace |
| ASL006 | Warning | Effective value contains `${NAME}`, `#{NAME}#`, `{{NAME}}`, or equals `CHANGE_ME` |

Duplicate detection covers collisions such as `{"Api":{"Url":"a"},"Api:Url":"b"}` and `{"Port":80,"port":81}`. Repeating a key **across files** is a normal override.

Comments and trailing commas are accepted. Arrays use zero-based keys such as `Servers:0:Host`. Later files replace individual keys, not whole sections or arrays: a shorter overlay array retains earlier higher indexes, as .NET's JSON configuration providers do.

## CLI options

```text
appsettings-lint <file.json> [overlay.json ...] [options]

--require <Section:Key>  Require a nonblank leaf value; repeat as needed.
--strict                 Fail on warnings as well as errors.
--format <text|json>     Output format; default: text.
--version                Print version.
--help, -h               Show help.
--                       Treat remaining arguments as file paths.
```

Exit codes: `0` passed, `1` lint findings, `2` invalid command arguments. A missing/unreadable input file is a lint error (`1`). JSON diagnostics go to standard output; argument errors go to standard error.

```sh
appsettings-lint appsettings.json --require Api:BaseUrl --format json
```

The JSON report contains `filesChecked`, `keysChecked`, `diagnostics`, `errors`, and `warnings`. Each diagnostic has a code, severity, file, optional key, and a value-free message.

## Source locations

ASL002, ASL005, and ASL006 include a `location` object with 1-based `line` and `column`. Duplicate findings additionally include `originalLocation`, pointing to the first occurrence in the same file. The fields are additive; unavailable locations are null. Codes, severity rules, exit codes, and report counts remain unchanged.

```json
{
  "code": "ASL006",
  "severity": "warning",
  "file": "appsettings.json",
  "key": "Api:Token",
  "message": "Unresolved placeholder in the effective value.",
  "location": { "line": 2, "column": 27 },
  "originalLocation": null
}
```

Locations identify the opening quote of the source property name, or the start of a directly indexed array value. For overlays, warnings and blank required findings point into the winning file. Nested and colon-containing keys keep their separate source positions even when they flatten to the same configuration key.

Columns count Unicode scalar values, not UTF-8 bytes or UTF-16 surrogate units; tabs count as one column. Line counting supports LF and CRLF, and a file's encoding BOM is excluded. Escaped property names are decoded for key identity while coordinates refer to their original written form. Parser syntax errors retain their existing byte-position message. No values or source snippets are added to diagnostics.

## Add to CI

After installing the tool in your build job:

```yaml
- name: Check production settings
  run: appsettings-lint appsettings.json appsettings.Production.json --require Api:BaseUrl --strict
```

Only require values that should exist **in the supplied JSON files**. Environment variables, user secrets, Key Vault, command-line configuration, and custom providers are not loaded. The tool does not validate options binding, value types, URLs, or connectivity, and does not replace startup validation.

For .NET provider behavior, see Microsoft's [configuration provider documentation](https://learn.microsoft.com/dotnet/core/extensions/configuration-providers).

## Compatibility fixtures

The test suite checks key flattening, comments, trailing commas, null/empty values, case-insensitive and colon-path collisions, empty property names, and file/array overlays against `Microsoft.Extensions.Configuration.Json 10.0.15` (test dependency only). The CLI retains zero runtime package dependencies. The provider stores an empty object as null and an empty array as an empty string; both are blank for this tool's required-value check.

One intentional lint difference: repeated empty objects/arrays at the same flattened key are reported as ASL002, even where the provider accepts and overwrites an empty-container marker. This stricter duplicate check is useful for review; it is not a claim that every linter error causes .NET startup to fail. The checks cover local JSON, not options binding or other configuration providers.

## Development and feedback

```sh
dotnet test -c Release
dotnet format --verify-no-changes
dotnet pack src/AppSettingsLint.Cli -c Release -o ./artifacts
```

For the CLI/Core boundaries, data flow, and completion scope, read the [design guide](docs/DESIGN.md). For installation, exit codes, placeholders, and coordinate interpretation, use the [troubleshooting guide](docs/TROUBLESHOOTING.md).

Have a configuration mistake this tool misses? [Open an issue](https://github.com/alirezaRanjbar4/appsettings-lint/issues/new/choose) with a **minimal, redacted** sample and the expected diagnostic. See [CONTRIBUTING.md](CONTRIBUTING.md) and the [roadmap](ROADMAP.md).

## راهنمای کوتاه فارسی

این ابزار خطاهای رایج فایل‌های `appsettings` را پیش از استقرار پیدا می‌کند: کلید تکراری، مقدار ضروریِ خالی و placeholder باقی‌مانده. فایل‌ها به ترتیب ورودی روی هم اعمال می‌شوند. مقدار تنظیمات در گزارش چاپ نمی‌شود. راه‌اندازی و نمونه‌های بالا با ‎.NET 10 در ویندوز، لینوکس و مک قابل اجرا هستند. برای گزارش مشکل، نمونه کوچک و بدون اطلاعات محرمانه بفرستید.

## License

[MIT](LICENSE)

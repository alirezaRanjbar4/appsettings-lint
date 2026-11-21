# Design and first-scope handoff

`appsettings-lint` checks local JSON files before deployment. Its bounded first scope is duplicate flattened keys, ordered overlays, required values, placeholder warnings, text/JSON reports, and source locations. It does not load an application or other configuration providers.

## Boundaries and data flow

`AppSettingsLint.Cli/CliApplication.cs` parses file arguments and options, calls Core, formats reports, and selects the exit code. `AppSettingsLint.Core/ConfigurationLinter.cs` owns reading, validation, flattening, layering, and lint rules. Core returns `LintReport`/`Diagnostic` records without writing to the console. Core temporarily holds values to check them; the public report model has no value field.

For each file, in command-line order:

1. Read the text, then validate the entire document with `JsonDocument`. Comments and trailing commas are accepted; the root must be an object.
2. Only after validation, walk the UTF-8 tokens using `Utf8JsonReader`. This prevents the source walker from operating on a partial or malformed document.
3. Build a per-file, case-insensitive dictionary of flattened settings. Keep the value, source file, and source location internally.
4. Copy those settings into the effective dictionary, replacing matching keys from earlier files.

Invalid or unreadable files produce ASL001/ASL003 and contribute no settings. Other files are still checked. After layering, check distinct required keys and scan effective values for placeholders. Errors discovered within an earlier file remain errors even when a later file overrides the key.

## Key identity and overlay provenance

Nested properties join with `:`; arrays add zero-based indexes. Thus `Api:Url` collides with nested `Api` → `Url`, and `Servers:0:Host` identifies one array item's property. Key comparison ignores case. Nonempty sections contribute their leaves; empty objects/arrays contribute a blank container marker. The root itself contributes no key.

Within a file, the first occurrence of a flattened key is retained and a later occurrence produces ASL002 with both coordinates. Across files, repetition is an ordinary override. Layering replaces individual flattened keys, so a shorter overlay array leaves earlier higher indexes present. Blank-required and placeholder findings use the winning setting's file and location; they do not point to an overridden value.

The JSON-provider fixtures compare against `Microsoft.Extensions.Configuration.Json 10.0.15`, a test-only dependency. One intentional difference is stricter linting of repeated empty-container markers. The linter is not a simulator of options binding or every provider's runtime behavior.

## Source locations and report contract

`SourceCoordinates` walks token offsets in source order. Locations are 1-based; columns count Unicode scalar values, with a tab counting as one column. LF and CRLF are supported and a decoded encoding BOM does not count. Property locations point to the opening quote; directly indexed array values point to their first token. Escaped names are decoded for key identity, while locations refer to the written JSON.

ASL002, ASL005, and ASL006 have `location`; ASL002 also has `originalLocation`. Unavailable locations are null. ASL001 syntax messages use a separate 1-based **byte** position, not the scalar-column convention. Diagnostics expose codes, severity, paths, keys, fixed messages, and optional locations; they omit values and source snippets. Paths and key names remain visible.

The CLI returns `0` for success, `1` for errors or strict-mode warnings, and `2` for argument errors. Lint reports go to stdout; argument errors go to stderr. A read failure is a lint error, not an argument error. See the [README](../README.md) for diagnostic codes and JSON fields, and [troubleshooting](TROUBLESHOOTING.md) for examples.

## Verification and completion boundary

Tests cover lint rules, CLI contracts, real-provider parity, Unicode coordinates, duplicates, and overlay provenance. CI tests, verifies formatting, and packages the CLI on Windows, Linux, and macOS. Use synthetic fixtures and keep parser errors value-free when changing behavior; see [CONTRIBUTING](../CONTRIBUTING.md).

This handoff completes the documentation part of the first scope. A validated closing release is still required before marking that scope complete. Extra providers, schemas, binding checks, SARIF, and NuGet.org publishing remain optional future work, not prerequisites. See the [roadmap](../ROADMAP.md).

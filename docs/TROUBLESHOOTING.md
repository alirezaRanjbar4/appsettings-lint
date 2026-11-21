# Troubleshooting

These commands target the current published package, **0.2.0**, and the repository's synthetic examples. Run from the repository root unless a command says otherwise. The [README](../README.md) lists all options and diagnostic codes; the [design guide](DESIGN.md) explains the data flow.

## Installation, updates, and command discovery

The package is on [GitHub Releases](https://github.com/alirezaRanjbar4/appsettings-lint/releases/tag/v0.2.0), not NuGet.org. Download `AppSettingsLint.Tool.0.2.0.nupkg` and `SHA256SUMS.txt` into an empty `downloaded-package` folder and compare its SHA-256 with the checksum file. A package-source error can mean the folder, package version, or filename is wrong. The tool requires .NET 10; check `dotnet --info` if it cannot start.

For an isolated installation, choose a **fresh** tool directory:

```sh
dotnet tool install AppSettingsLint.Tool --version 0.2.0 --tool-path ./artifacts/handoff-tool --add-source ./downloaded-package
```

Run `./artifacts/handoff-tool/appsettings-lint --version` on Linux/macOS, or `.\artifacts\handoff-tool\appsettings-lint.exe --version` in PowerShell. This avoids depending on the global tool PATH. If the global installation is not found, check `dotnet tool list --global` and whether the .NET tools directory is on PATH (`~/.dotnet/tools`, or `%USERPROFILE%\.dotnet\tools` on Windows); open a new terminal after changing PATH.

For an **existing** isolated installation, use `update` with the same tool path:

```sh
dotnet tool update AppSettingsLint.Tool --version 0.2.0 --tool-path ./artifacts/handoff-tool --add-source ./downloaded-package
```

For a global installation, use `--global` in place of `--tool-path ...`. If the command still reports an older version, invoke the intended executable by its full path and check for another global/local installation. The commands below use `appsettings-lint` as shorthand for that executable.

## Understand exit codes before changing CI

| Exit | Meaning | Where to look |
| --- | --- | --- |
| `0` | No errors; warnings are allowed unless `--strict` is set | stdout report |
| `1` | Lint errors, or warnings with `--strict` | stdout diagnostics |
| `2` | Invalid command arguments | stderr usage message |

```sh
appsettings-lint examples/invalid.json --require Api:BaseUrl --strict
appsettings-lint examples/appsettings.json examples/appsettings.Production.json --require Api:Token --strict
```

The first exits `1` with ASL002, ASL005, and ASL006. The second exits `0`. `--format xml` is an argument error (`2`), while a missing input file is ASL003 (`1`). When saving reports, use a fresh destination outside the input files; JSON reports go to stdout, so shell redirection is your shell's operation.

## ASL001 / ASL003: JSON or file problems

Use an existing, readable file path relative to the current directory, and quote paths containing spaces. The JSON root must be an object. Comments and trailing commas are supported, but truncated documents and invalid tokens are not. Syntax messages omit source text and report a 1-based line and byte position. A filename starting with `-` can be passed after `--`; all arguments after that marker are file paths.

## ASL002: duplicate flattened keys

Search for case-only duplicates and colon-path collisions, including a literal `Api:Url` alongside a nested `Api` object containing `Url`. Rename or remove the duplicate **within that file**. A later overlay does not erase an earlier file's duplicate error. Repeating a key across separate input files is a supported override. `originalLocation` points to the first same-file occurrence, and `location` points to the later one.

## ASL004 / ASL005: required values and environment settings

Use a leaf key such as `Api:BaseUrl`, not the nonempty `Api` section. ASL004 means the flattened key is absent after all files are layered; ASL005 means its effective value is null/blank, including an empty-container marker. Check file order and the winning overlay.

Environment variables, user secrets, Key Vault, command-line application configuration, and custom providers are not loaded. Setting an environment variable does not satisfy `--require` or resolve a placeholder. Require only settings intended to exist in the supplied JSON; validate environment-supplied settings in the application's startup validation.

## ASL006: placeholders and unexpected warnings

The effective value matches `${NAME}`, `#{NAME}#`, `{{NAME}}`, or a case-insensitive, trimmed `CHANGE_ME`. The tool detects these forms; it does not substitute them. Base placeholders replaced by a later JSON file no longer warn. Without `--strict`, warnings alone return `0`; with it, they return `1`.

```sh
appsettings-lint examples/appsettings.json --format json
```

This exits `0` but reports the base `Api:Token` placeholder. Its source is the base file; adding the production overlay resolves it. A shorter array overlay may retain earlier higher indexes, so inspect the affected flattened key rather than assuming the whole array was replaced.

## Line/column differs from an editor or parser message

ASL002/005/006 locations use Unicode scalar columns, not UTF-8 bytes, UTF-16 units, or visual tab stops. An emoji counts as one column and a tab as one. Coordinates point to property names (or directly indexed array values), not necessarily the value text. LF/CRLF and encoding BOMs are handled. JSON escape sequences retain their written width. ASL001's byte-position message follows the parser's separate convention; unavailable structured locations are null.

If a result still looks wrong, share the OS, `--version`, command, expected code, and a minimal synthetic JSON sample in an [issue](https://github.com/alirezaRanjbar4/appsettings-lint/issues/new/choose). Reports omit values but retain paths and key names; inspect those before sharing. See [CONTRIBUTING](../CONTRIBUTING.md).

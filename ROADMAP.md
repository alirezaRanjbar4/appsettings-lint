# Roadmap

The first release focuses on a small, dependable JSON check that can run before deployment.

## Available in v0.1.0

- Duplicate flattened-key detection, including case-only collisions.
- Ordered JSON overlays, required leaf keys, and placeholder warnings.
- Text and JSON reports with stable diagnostic codes and exit codes.
- Source installation and a downloadable .NET tool package.
- Windows, Linux, and macOS CI.

## Delivered in v0.1.1

Added 13 fixtures against the actual .NET 10 JSON provider and documented the intentional stricter empty-container duplicate check. CLI lint behavior is unchanged.

## Delivered in v0.2.1

The [design guide](docs/DESIGN.md) and [troubleshooting guide](docs/TROUBLESHOOTING.md) document the architecture, CLI/report contracts, installation, common findings, and provenance/coordinate rules. The tool package includes these guides and their linked root documents. Commands target v0.2.1.

## First-scope completion

The bounded scope consists of the delivered JSON checks, overlays, provider fixtures, source diagnostics, and user/developer guides. [v0.2.1](https://github.com/alirezaRanjbar4/appsettings-lint/releases/tag/v0.2.1) is the closing release for that scope, with packaged installation/examples, output contracts and supported-platform CI checked as release acceptance. The project remains available for bug fixes and feedback; optional enhancements below do not reopen or extend the completed scope.

## Future candidates, driven by feedback

- Extend provider fixtures when new edge cases are reported.
- Improve installation discovery and consider NuGet.org distribution when publishing credentials are available.
- Evaluate SARIF output for GitHub code scanning if users need it.

Features are planned, not promises of release dates. Please describe a concrete use case in an issue.

## Delivered in v0.2.0

- Source locations for duplicate-key, placeholder, and blank-required findings, including original duplicate occurrences.
- Additive JSON coordinates and Unicode-aware text diagnostics, with regression tests for overlays and flattened collisions.

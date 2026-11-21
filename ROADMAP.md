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

## Documentation handoff on main

The [design guide](docs/DESIGN.md) and [troubleshooting guide](docs/TROUBLESHOOTING.md) document the architecture, CLI/report contracts, installation, common findings, and provenance/coordinate rules. Their commands target the current published version, v0.2.0.

## First-scope completion

The bounded scope consists of the delivered JSON checks, overlays, provider fixtures, source diagnostics, and user/developer guides. A tested closing release that includes the handoff documentation is the remaining required step. Validate its packaged installation/examples and supported-platform CI before marking the first scope complete. Keep the repository available for fixes and feedback afterward.

## Future candidates, driven by feedback

- Extend provider fixtures when new edge cases are reported.
- Improve installation discovery and consider NuGet.org distribution when publishing credentials are available.
- Evaluate SARIF output for GitHub code scanning if users need it.

Features are planned, not promises of release dates. Please describe a concrete use case in an issue.

## Delivered in v0.2.0

- Source locations for duplicate-key, placeholder, and blank-required findings, including original duplicate occurrences.
- Additive JSON coordinates and Unicode-aware text diagnostics, with regression tests for overlays and flattened collisions.

# Roadmap

The first release focuses on a small, dependable JSON check that can run before deployment.

## Available in v0.1.0

- Duplicate flattened-key detection, including case-only collisions.
- Ordered JSON overlays, required leaf keys, and placeholder warnings.
- Text and JSON reports with stable diagnostic codes and exit codes.
- Source installation and a downloadable .NET tool package.
- Windows, Linux, and macOS CI.

## Delivered in v0.1.1

Added 12 fixtures against the actual .NET 10 JSON provider and documented the intentional stricter empty-container duplicate check. CLI lint behavior is unchanged.

## Next, driven by feedback

- Add source line/column locations for duplicate-key and placeholder findings without exposing values.
- Extend provider fixtures when new edge cases are reported.
- Improve installation discovery and consider NuGet.org distribution when publishing credentials are available.
- Evaluate SARIF output for GitHub code scanning if users need it.

Features are planned, not promises of release dates. Please describe a concrete use case in an issue.

# Roadmap

The first release focuses on a small, dependable JSON check that can run before deployment.

## Available in v0.1.0

- Duplicate flattened-key detection, including case-only collisions.
- Ordered JSON overlays, required leaf keys, and placeholder warnings.
- Text and JSON reports with stable diagnostic codes and exit codes.
- Source installation and a downloadable .NET tool package.
- Windows, Linux, and macOS CI.

## Next, driven by feedback

- Add source line/column locations for duplicate-key and placeholder findings without exposing values.
- Test more provider edge cases against Microsoft's actual JSON provider.
- Improve installation discovery and consider NuGet.org distribution when publishing credentials are available.
- Evaluate SARIF output for GitHub code scanning if users need it.

Features are planned, not promises of release dates. Please describe a concrete use case in an issue.

# Changelog

## 0.2.0 — 2026-11-21

Added value-free, 1-based source coordinates for duplicate keys, placeholder warnings, and blank required settings. Duplicate findings include both occurrences; JSON fields are additive. Text diagnostics use file(line,column) locations. Coordinates handle Unicode scalars, escaped keys, comments, arrays, BOMs, CRLF, and file overlays.

## 0.1.1 — 2026-11-21

Added 13 fixtures against the actual .NET 10 JSON provider and documented the intentional stricter empty-container duplicate check. CLI lint behavior is unchanged.

## 0.1.0 — 2026-11-20

Initial release: JSON syntax checks, duplicate keys, ordered file overlays, required leaf values, unresolved-placeholder warnings, value-free text/JSON output, and .NET tool packaging.

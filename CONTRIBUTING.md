# Contributing

Bug reports, small fixes, and real-world configuration examples are welcome.

1. Check existing issues and explain the problem before proposing a large feature.
2. Share only small, synthetic or redacted JSON samples. Never paste credentials.
3. Add regression tests for changed lint behavior and CLI exit codes.
4. Run `dotnet test -c Release` and `dotnet format --verify-no-changes`.
5. Keep diagnostics free of configuration values, including parser exceptions.

The CLI currently checks local JSON inputs only. New providers or schema features should be discussed first so that the scope remains easy to understand.

Pull requests should describe the user-visible change and the verification performed. Be respectful and specific when reviewing someone else's work.

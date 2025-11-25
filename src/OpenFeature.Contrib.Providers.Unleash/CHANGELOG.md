# Changelog

## [0.0.1](https://github.com/open-feature/dotnet-sdk-contrib/releases/tag/OpenFeature.Contrib.Providers.Unleash-0.0.1)

Initial release of the Unleash OpenFeature Provider for .NET.

### ✨ New Features

* Boolean evaluation using Unleash `IsEnabled`
* String, integer, double, and object evaluation using Unleash variants
* Context transformation from OpenFeature EvaluationContext to Unleash UnleashContext
* Support for well-known context keys: UserId, SessionId, RemoteAddress, AppName, Environment, CurrentTime
* Support for custom properties in evaluation context

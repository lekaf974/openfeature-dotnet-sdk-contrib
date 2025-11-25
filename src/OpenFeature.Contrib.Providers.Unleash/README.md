# Unleash OpenFeature Provider for .NET

[Unleash](https://getunleash.io) OpenFeature Provider enables the use of Unleash feature flags with the OpenFeature .NET SDK.

## Installation

```shell
dotnet add package OpenFeature.Contrib.Providers.Unleash
```

## Concepts

* **Boolean evaluation** uses Unleash's `IsEnabled` to determine if a feature toggle is enabled.
* **String/Integer/Double/Object evaluation** uses Unleash's `GetVariant` to retrieve variant values.

## Usage

### Basic Usage

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Unleash;
using Unleash;

// Configure Unleash settings
var settings = new UnleashSettings
{
    AppName = "my-app",
    UnleashApi = new Uri("https://unleash.example.com/api/"),
    CustomHttpHeaders = new Dictionary<string, string>
    {
        { "Authorization", "*:development.your-api-token" }
    }
};

// Create provider configuration
var config = new UnleashProviderConfiguration(settings);
var provider = new UnleashProvider(config);

// Set the provider
await OpenFeature.Api.Instance.SetProviderAsync(provider);

// Get a client
var client = OpenFeature.Api.Instance.GetClient();

// Evaluate a boolean flag
var isEnabled = await client.GetBooleanValueAsync("my-feature", false);

// Evaluate with context
var context = EvaluationContext.Builder()
    .Set("UserId", "user-123")
    .Set("SessionId", "session-456")
    .Set("RemoteAddress", "192.168.1.1")
    .Set("Environment", "production")
    .Set("AppName", "my-app")
    .Set("customProperty", "custom-value")
    .Build();

var isEnabledWithContext = await client.GetBooleanValueAsync("my-feature", false, context);

// Get variant value as string
var variantValue = await client.GetStringValueAsync("my-variant-flag", "default-value", context);
```

### Using Pre-configured Unleash Client

If you need more control over the Unleash client configuration, you can create the client yourself:

```csharp
using OpenFeature;
using OpenFeature.Contrib.Providers.Unleash;
using Unleash;

// Create your own Unleash client with custom configuration
var settings = new UnleashSettings
{
    AppName = "my-app",
    UnleashApi = new Uri("https://unleash.example.com/api/"),
    // Add more custom settings...
};

var unleashClient = new DefaultUnleash(settings);

// Create provider with the pre-configured client
var provider = new UnleashProvider(unleashClient);

await OpenFeature.Api.Instance.SetProviderAsync(provider);
```

## Context Properties

The provider transforms OpenFeature evaluation context to Unleash context. The following well-known properties are mapped:

| OpenFeature Context Key | Unleash Context Property |
|------------------------|-------------------------|
| `UserId`               | `UserId`                |
| `SessionId`            | `SessionId`             |
| `RemoteAddress`        | `RemoteAddress`         |
| `AppName`              | `AppName`               |
| `Environment`          | `Environment`           |
| `CurrentTime`          | `CurrentTime`           |
| `targetingKey`         | `UserId` (if UserId not set) |

All other properties are added to the Unleash context's `Properties` dictionary.

## Additional Evaluation Data

The provider includes the following data in flag metadata:

* `enabled` - Boolean indicating if the toggle/variant is enabled
* `payload-type` - String indicating the payload type (only for variant evaluations with payloads)

## Notes

* When a variant is disabled, the default value is returned without a variant name.
* JSON/CSV payloads from variants are returned as strings wrapped in a Value object.
* Integer and double evaluations will attempt to parse string variant payloads.

## See Also

* [Unleash Documentation](https://docs.getunleash.io/)
* [Unleash .NET Client](https://github.com/Unleash/unleash-client-dotnet)
* [OpenFeature Documentation](https://openfeature.dev/)

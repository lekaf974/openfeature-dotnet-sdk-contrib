using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;

namespace OpenFeature.Contrib.Providers.Unleash;

/// <summary>
/// An OpenFeature provider that integrates with Unleash feature flag management.
/// </summary>
/// <remarks>
/// The Unleash provider supports boolean evaluation using <c>IsEnabled</c> and variant-based
/// evaluation for string, integer, double, and object types using <c>GetVariant</c>.
/// </remarks>
/// <example>
/// <code>
/// var settings = new UnleashSettings
/// {
///     AppName = "my-app",
///     UnleashApi = new Uri("https://unleash.example.com/api/"),
///     CustomHttpHeaders = new Dictionary&lt;string, string&gt;
///     {
///         { "Authorization", "API_KEY" }
///     }
/// };
/// var config = new UnleashProviderConfiguration(settings);
/// var provider = new UnleashProvider(config);
///
/// await OpenFeature.Api.Instance.SetProviderAsync(provider);
/// var client = OpenFeature.Api.Instance.GetClient();
///
/// var isEnabled = await client.GetBooleanValueAsync("my-feature", false);
/// </code>
/// </example>
public class UnleashProvider : FeatureProvider
{
    private static readonly Metadata ProviderMetadata = new("Unleash Provider");

    internal readonly IUnleash _unleashClient;
    private readonly UnleashProviderConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="UnleashProvider"/> using the provided configuration.
    /// The Unleash client will be created using the settings from the configuration.
    /// </summary>
    /// <param name="configuration">The provider configuration containing Unleash settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public UnleashProvider(UnleashProviderConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _unleashClient = new DefaultUnleash(configuration.UnleashSettings);
    }

    /// <summary>
    /// Creates a new instance of <see cref="UnleashProvider"/> using a pre-configured Unleash client.
    /// Use this constructor when you need more control over the Unleash client configuration.
    /// </summary>
    /// <param name="unleashClient">A pre-configured Unleash client instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when unleashClient is null.</exception>
    public UnleashProvider(IUnleash unleashClient)
    {
        _unleashClient = unleashClient ?? throw new ArgumentNullException(nameof(unleashClient));
    }

    /// <inheritdoc/>
    public override Metadata GetMetadata() => ProviderMetadata;

    /// <inheritdoc/>
    /// <remarks>
    /// This method uses Unleash's <c>IsEnabled</c> to evaluate boolean flags.
    /// The evaluation context is transformed to an Unleash context using <see cref="ContextTransformer"/>.
    /// </remarks>
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = default)
    {
        var unleashContext = ContextTransformer.Transform(context);
        var isEnabled = _unleashClient.IsEnabled(flagKey, unleashContext, defaultValue);

        var flagMetadata = new ImmutableMetadata(
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "enabled", isEnabled }
            });

        return Task.FromResult(new ResolutionDetails<bool>(
            flagKey,
            isEnabled,
            flagMetadata: flagMetadata));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// String evaluation uses Unleash's variant feature. If the variant is disabled,
    /// the default value is returned.
    /// </remarks>
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = default)
    {
        var objectResult = ResolveObjectValue(flagKey, new Value(defaultValue), context);

        return Task.FromResult(new ResolutionDetails<string>(
            flagKey,
            objectResult.Value?.AsString ?? defaultValue,
            variant: objectResult.Variant,
            reason: objectResult.Reason,
            errorType: objectResult.ErrorType,
            errorMessage: objectResult.ErrorMessage,
            flagMetadata: objectResult.FlagMetadata));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Integer evaluation uses Unleash's variant feature. The variant payload is parsed as an integer.
    /// If parsing fails or the variant is disabled, the default value is returned.
    /// </remarks>
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = default)
    {
        var objectResult = ResolveObjectValue(flagKey, new Value(defaultValue), context);

        if (objectResult.Value?.IsNumber == true && objectResult.Value?.AsInteger.HasValue == true)
        {
            return Task.FromResult(new ResolutionDetails<int>(
                flagKey,
                objectResult.Value.AsInteger.Value,
                variant: objectResult.Variant,
                reason: objectResult.Reason,
                errorType: objectResult.ErrorType,
                errorMessage: objectResult.ErrorMessage,
                flagMetadata: objectResult.FlagMetadata));
        }

        // Try to parse the string value as integer
        if (objectResult.Value?.IsString == true &&
            int.TryParse(objectResult.Value.AsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return Task.FromResult(new ResolutionDetails<int>(
                flagKey,
                intValue,
                variant: objectResult.Variant,
                reason: objectResult.Reason,
                errorType: objectResult.ErrorType,
                errorMessage: objectResult.ErrorMessage,
                flagMetadata: objectResult.FlagMetadata));
        }

        // Return default value if variant is disabled or parsing fails
        if (objectResult.Variant == null || objectResult.Value == null)
        {
            return Task.FromResult(new ResolutionDetails<int>(
                flagKey,
                defaultValue,
                reason: objectResult.Reason,
                flagMetadata: objectResult.FlagMetadata));
        }

        throw new TypeMismatchException($"Failed to parse variant value as integer for flag '{flagKey}'");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Double evaluation uses Unleash's variant feature. The variant payload is parsed as a double.
    /// If parsing fails or the variant is disabled, the default value is returned.
    /// </remarks>
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = default)
    {
        var objectResult = ResolveObjectValue(flagKey, new Value(defaultValue), context);

        if (objectResult.Value?.IsNumber == true && objectResult.Value?.AsDouble.HasValue == true)
        {
            return Task.FromResult(new ResolutionDetails<double>(
                flagKey,
                objectResult.Value.AsDouble.Value,
                variant: objectResult.Variant,
                reason: objectResult.Reason,
                errorType: objectResult.ErrorType,
                errorMessage: objectResult.ErrorMessage,
                flagMetadata: objectResult.FlagMetadata));
        }

        // Try to parse the string value as double
        if (objectResult.Value?.IsString == true &&
            double.TryParse(objectResult.Value.AsString, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return Task.FromResult(new ResolutionDetails<double>(
                flagKey,
                doubleValue,
                variant: objectResult.Variant,
                reason: objectResult.Reason,
                errorType: objectResult.ErrorType,
                errorMessage: objectResult.ErrorMessage,
                flagMetadata: objectResult.FlagMetadata));
        }

        // Return default value if variant is disabled or parsing fails
        if (objectResult.Variant == null || objectResult.Value == null)
        {
            return Task.FromResult(new ResolutionDetails<double>(
                flagKey,
                defaultValue,
                reason: objectResult.Reason,
                flagMetadata: objectResult.FlagMetadata));
        }

        throw new TypeMismatchException($"Failed to parse variant value as double for flag '{flagKey}'");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Object evaluation uses Unleash's variant feature. The variant payload is returned as a Value object.
    /// If the variant is disabled, the default value is returned.
    /// </remarks>
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext context = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResolveObjectValue(flagKey, defaultValue, context));
    }

    private ResolutionDetails<Value> ResolveObjectValue(string flagKey, Value defaultValue, EvaluationContext context)
    {
        var unleashContext = ContextTransformer.Transform(context);
        var variant = _unleashClient.GetVariant(flagKey, unleashContext);

        var flagMetadata = new ImmutableMetadata(
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "enabled", variant.Enabled }
            });

        // Check if variant is disabled
        if (!variant.Enabled || variant.Name == "disabled")
        {
            return new ResolutionDetails<Value>(
                flagKey,
                defaultValue,
                flagMetadata: flagMetadata);
        }

        // Get the payload value
        Value value = defaultValue;
        string variantName = variant.Name;

        if (variant.Payload != null && !string.IsNullOrEmpty(variant.Payload.Value))
        {
            value = new Value(variant.Payload.Value);

            // Add payload type to metadata if available
            if (!string.IsNullOrEmpty(variant.Payload.Type))
            {
                flagMetadata = new ImmutableMetadata(
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "enabled", variant.Enabled },
                        { "payload-type", variant.Payload.Type }
                    });
            }
        }

        return new ResolutionDetails<Value>(
            flagKey,
            value,
            variant: variantName,
            flagMetadata: flagMetadata);
    }

    /// <inheritdoc/>
    public override Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (!_disposed)
        {
            _unleashClient?.Dispose();
            _disposed = true;
        }
        return Task.CompletedTask;
    }
}

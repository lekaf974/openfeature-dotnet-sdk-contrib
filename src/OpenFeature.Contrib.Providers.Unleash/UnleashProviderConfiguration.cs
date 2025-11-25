using System;
using Unleash;

namespace OpenFeature.Contrib.Providers.Unleash;

/// <summary>
/// Configuration options for the Unleash OpenFeature provider.
/// </summary>
public class UnleashProviderConfiguration
{
    /// <summary>
    /// Gets the Unleash settings used to configure the Unleash client.
    /// </summary>
    public UnleashSettings UnleashSettings { get; }

    /// <summary>
    /// Creates a new instance of <see cref="UnleashProviderConfiguration"/>.
    /// </summary>
    /// <param name="unleashSettings">The Unleash settings to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when unleashSettings is null.</exception>
    public UnleashProviderConfiguration(UnleashSettings unleashSettings)
    {
        UnleashSettings = unleashSettings ?? throw new ArgumentNullException(nameof(unleashSettings));
    }
}

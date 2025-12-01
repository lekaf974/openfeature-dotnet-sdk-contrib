using System;
using Unleash;

namespace OpenFeature.Contrib.Providers.Unleash;

public class UnleashProviderConfiguration
{
    public UnleashSettings UnleashSettings { get; }

    public UnleashProviderConfiguration(UnleashSettings unleashSettings)
    {
        UnleashSettings = unleashSettings ?? throw new ArgumentNullException(nameof(unleashSettings));
    }
}

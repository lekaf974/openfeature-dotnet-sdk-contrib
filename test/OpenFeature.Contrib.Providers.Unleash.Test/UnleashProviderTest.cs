using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;
using Unleash;
using Xunit;
using Yggdrasil;

using Variant = Unleash.Internal.Variant;

namespace OpenFeature.Contrib.Providers.Unleash.Test;

public class UnleashProviderTest
{
    private static UnleashSettings GetDefaultUnleashSettings() => new UnleashSettings
    {
        AppName = "test-app",
        UnleashApi = new Uri("https://unleash.example.com/api/")
    };

    [Fact]
    public void CreateUnleashProvider_WithValidConfiguration_CreatesProviderInstanceSuccessfully()
    {
        // Arrange
        var settings = GetDefaultUnleashSettings();
        var config = new UnleashProviderConfiguration(settings);

        // Act
        var provider = new UnleashProvider(config);

        // Assert
        Assert.NotNull(provider._unleashClient);
    }

    [Fact]
    public void CreateUnleashProvider_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnleashProvider((UnleashProviderConfiguration)null));
    }

    [Fact]
    public void CreateUnleashProvider_WithUnleashClient_CreatesProviderInstanceSuccessfully()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();

        // Act
        var provider = new UnleashProvider(mockClient);

        // Assert
        Assert.NotNull(provider._unleashClient);
        Assert.Same(mockClient, provider._unleashClient);
    }

    [Fact]
    public void CreateUnleashProvider_WithNullUnleashClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnleashProvider((IUnleash)null));
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectMetadata()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var provider = new UnleashProvider(mockClient);

        // Act
        var metadata = provider.GetMetadata();

        // Assert
        Assert.Equal("Unleash Provider", metadata.Name);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureEnabled_ReturnsTrue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        mockClient.IsEnabled("test-feature", Arg.Any<UnleashContext>(), Arg.Any<bool>()).Returns(true);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveBooleanValueAsync("test-feature", false);

        // Assert
        Assert.True(result.Value);
        Assert.Equal("test-feature", result.FlagKey);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WhenFeatureDisabled_ReturnsFalse()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        mockClient.IsEnabled("test-feature", Arg.Any<UnleashContext>(), Arg.Any<bool>()).Returns(false);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveBooleanValueAsync("test-feature", true);

        // Assert
        Assert.False(result.Value);
        Assert.Equal("test-feature", result.FlagKey);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_WithEvaluationContext_PassesContextToUnleash()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        mockClient.IsEnabled("test-feature", Arg.Is<UnleashContext>(ctx =>
            ctx.UserId == "user-123" &&
            ctx.SessionId == "session-456"), Arg.Any<bool>()).Returns(true);
        var provider = new UnleashProvider(mockClient);

        var context = EvaluationContext.Builder()
            .Set("UserId", "user-123")
            .Set("SessionId", "session-456")
            .Build();

        // Act
        var result = await provider.ResolveBooleanValueAsync("test-feature", false, context);

        // Assert
        Assert.True(result.Value);
    }

    [Fact]
    public async Task ResolveBooleanValueAsync_ContainsEnabledMetadata()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        mockClient.IsEnabled("test-feature", Arg.Any<UnleashContext>(), Arg.Any<bool>()).Returns(true);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveBooleanValueAsync("test-feature", false);

        // Assert
        Assert.NotNull(result.FlagMetadata);
        Assert.True(result.FlagMetadata.GetBool("enabled"));
    }

    [Fact]
    public async Task ResolveStringValueAsync_WithEnabledVariant_ReturnsVariantValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("string", "test-value"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveStringValueAsync("test-feature", "default");

        // Assert
        Assert.Equal("test-value", result.Value);
        Assert.Equal("variant-1", result.Variant);
        Assert.Equal("test-feature", result.FlagKey);
    }

    [Fact]
    public async Task ResolveStringValueAsync_WithDisabledVariant_ReturnsDefaultValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("disabled", null, false, false);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveStringValueAsync("test-feature", "default-value");

        // Assert
        Assert.Equal("default-value", result.Value);
        Assert.Null(result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WithValidIntegerPayload_ReturnsIntegerValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("number", "42"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveIntegerValueAsync("test-feature", 0);

        // Assert
        Assert.Equal(42, result.Value);
        Assert.Equal("variant-1", result.Variant);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WithDisabledVariant_ReturnsDefaultValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("disabled", null, false, false);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveIntegerValueAsync("test-feature", 100);

        // Assert
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task ResolveIntegerValueAsync_WithInvalidPayload_ThrowsTypeMismatchException()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("string", "not-a-number"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act & Assert
        await Assert.ThrowsAsync<TypeMismatchException>(() =>
            provider.ResolveIntegerValueAsync("test-feature", 0));
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WithValidDoublePayload_ReturnsDoubleValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("number", "3.14"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveDoubleValueAsync("test-feature", 0.0);

        // Assert
        Assert.Equal(3.14, result.Value);
        Assert.Equal("variant-1", result.Variant);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WithDisabledVariant_ReturnsDefaultValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("disabled", null, false, false);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act
        var result = await provider.ResolveDoubleValueAsync("test-feature", 99.99);

        // Assert
        Assert.Equal(99.99, result.Value);
    }

    [Fact]
    public async Task ResolveDoubleValueAsync_WithInvalidPayload_ThrowsTypeMismatchException()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("string", "not-a-number"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);

        // Act & Assert
        await Assert.ThrowsAsync<TypeMismatchException>(() =>
            provider.ResolveDoubleValueAsync("test-feature", 0.0));
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WithEnabledVariant_ReturnsVariantValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("json", "{\"key\":\"value\"}"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);
        var defaultValue = new Value("default");

        // Act
        var result = await provider.ResolveStructureValueAsync("test-feature", defaultValue);

        // Assert
        Assert.Equal("{\"key\":\"value\"}", result.Value.AsString);
        Assert.Equal("variant-1", result.Variant);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_WithDisabledVariant_ReturnsDefaultValue()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("disabled", null, false, false);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);
        var defaultValue = new Value("default");

        // Act
        var result = await provider.ResolveStructureValueAsync("test-feature", defaultValue);

        // Assert
        Assert.Same(defaultValue, result.Value);
    }

    [Fact]
    public async Task ResolveStructureValueAsync_ContainsPayloadTypeMetadata()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var variant = new Variant("variant-1", new Payload("json", "{\"key\":\"value\"}"), true, true);
        mockClient.GetVariant("test-feature", Arg.Any<UnleashContext>()).Returns(variant);
        var provider = new UnleashProvider(mockClient);
        var defaultValue = new Value("default");

        // Act
        var result = await provider.ResolveStructureValueAsync("test-feature", defaultValue);

        // Assert
        Assert.NotNull(result.FlagMetadata);
        Assert.Equal("json", result.FlagMetadata.GetString("payload-type"));
    }

    [Fact]
    public async Task ShutdownAsync_DisposesUnleashClient()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var provider = new UnleashProvider(mockClient);

        // Act
        await provider.ShutdownAsync();

        // Assert
        mockClient.Received(1).Dispose();
    }

    [Fact]
    public async Task ShutdownAsync_CalledTwice_DisposesOnlyOnce()
    {
        // Arrange
        var mockClient = Substitute.For<IUnleash>();
        var provider = new UnleashProvider(mockClient);

        // Act
        await provider.ShutdownAsync();
        await provider.ShutdownAsync();

        // Assert
        mockClient.Received(1).Dispose();
    }
}

public class ContextTransformerTest
{
    [Fact]
    public void Transform_WithNullContext_ReturnsEmptyUnleashContext()
    {
        // Act
        var result = ContextTransformer.Transform(null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.UserId);
    }

    [Fact]
    public void Transform_WithUserId_MapsToUnleashContext()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("UserId", "user-123")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("user-123", result.UserId);
    }

    [Fact]
    public void Transform_WithSessionId_MapsToUnleashContext()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("SessionId", "session-456")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("session-456", result.SessionId);
    }

    [Fact]
    public void Transform_WithRemoteAddress_MapsToUnleashContext()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("RemoteAddress", "192.168.1.1")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("192.168.1.1", result.RemoteAddress);
    }

    [Fact]
    public void Transform_WithAppName_MapsToUnleashContext()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("AppName", "my-app")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("my-app", result.AppName);
    }

    [Fact]
    public void Transform_WithEnvironment_MapsToUnleashContext()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("Environment", "production")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("production", result.Environment);
    }

    [Fact]
    public void Transform_WithCurrentTime_MapsToUnleashContext()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var context = EvaluationContext.Builder()
            .Set("CurrentTime", dateTime.ToString("O"))
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal(dateTime, result.CurrentTime);
    }

    [Fact]
    public void Transform_WithCustomProperties_MapsToPropertiesDictionary()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("customProp1", "value1")
            .Set("customProp2", "value2")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("value1", result.Properties["customProp1"]);
        Assert.Equal("value2", result.Properties["customProp2"]);
    }

    [Fact]
    public void Transform_WithTargetingKey_MapsToUserIdIfNotSet()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .SetTargetingKey("targeting-key-123")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("targeting-key-123", result.UserId);
    }

    [Fact]
    public void Transform_WithUserIdAndTargetingKey_UsesUserId()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("UserId", "explicit-user-id")
            .SetTargetingKey("targeting-key-123")
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("explicit-user-id", result.UserId);
    }

    [Fact]
    public void Transform_WithNumericValue_ConvertsToString()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("numericProp", 42)
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("42", result.Properties["numericProp"]);
    }

    [Fact]
    public void Transform_WithBooleanValue_ConvertsToLowercaseString()
    {
        // Arrange
        var context = EvaluationContext.Builder()
            .Set("boolProp", true)
            .Build();

        // Act
        var result = ContextTransformer.Transform(context);

        // Assert
        Assert.Equal("true", result.Properties["boolProp"]);
    }
}

public class UnleashProviderConfigurationTest
{
    [Fact]
    public void Constructor_WithValidSettings_CreatesConfiguration()
    {
        // Arrange
        var settings = new UnleashSettings
        {
            AppName = "test-app",
            UnleashApi = new Uri("https://unleash.example.com/api/")
        };

        // Act
        var config = new UnleashProviderConfiguration(settings);

        // Assert
        Assert.Same(settings, config.UnleashSettings);
    }

    [Fact]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnleashProviderConfiguration(null));
    }
}

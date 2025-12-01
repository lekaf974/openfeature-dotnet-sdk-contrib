using OpenFeature.Model;
using Unleash;

namespace OpenFeature.Contrib.Providers.Unleash;

internal static class ContextTransformer
{
    public const string ContextAppName = "AppName";
    public const string ContextUserId = "UserId";
    public const string ContextEnvironment = "Environment";
    public const string ContextRemoteAddress = "RemoteAddress";
    public const string ContextSessionId = "SessionId";
    public const string ContextCurrentTime = "CurrentTime";

    public static UnleashContext Transform(EvaluationContext context)
    {
        if (context == null)
        {
            return new UnleashContext();
        }

        var unleashContext = new UnleashContext();

        foreach (var kvp in context.AsDictionary())
        {
            var key = kvp.Key;
            var value = ConvertValueToString(kvp.Value);

            switch (key)
            {
                case ContextAppName:
                    unleashContext.AppName = value;
                    break;
                case ContextUserId:
                    unleashContext.UserId = value;
                    break;
                case ContextEnvironment:
                    unleashContext.Environment = value;
                    break;
                case ContextRemoteAddress:
                    unleashContext.RemoteAddress = value;
                    break;
                case ContextSessionId:
                    unleashContext.SessionId = value;
                    break;
                case ContextCurrentTime:
                    if (System.DateTimeOffset.TryParse(value, out var dateTimeOffset))
                    {
                        unleashContext.CurrentTime = dateTimeOffset;
                    }
                    break;
                default:
                    if (value != null)
                    {
                        unleashContext.Properties[key] = value;
                    }
                    break;
            }
        }

        // Use targeting key as UserId if not already set
        if (string.IsNullOrEmpty(unleashContext.UserId) && !string.IsNullOrEmpty(context.TargetingKey))
        {
            unleashContext.UserId = context.TargetingKey;
        }

        return unleashContext;
    }

    private static string ConvertValueToString(Value value)
    {
        if (value == null || value.IsNull)
        {
            return null;
        }

        if (value.IsString)
        {
            return value.AsString;
        }

        if (value.IsNumber)
        {
            return value.AsDouble?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value.IsBoolean)
        {
            return value.AsBoolean?.ToString().ToLowerInvariant();
        }

        // For other types, use the object's ToString
        return value.AsObject?.ToString();
    }
}

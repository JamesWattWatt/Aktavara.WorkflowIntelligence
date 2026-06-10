namespace Aktavara.WorkflowIntelligence.Core.Services;

/// <summary>
/// Detects the format of activity log payloads.
/// </summary>
public enum PayloadType
{
    Unknown,
    Xml,
    Json,
    BooleanResult
}

/// <summary>
/// Utility for detecting payload format.
/// </summary>
public static class PayloadTypeDetector
{
    /// <summary>
    /// Detects whether a payload is XML or JSON based on the first non-whitespace character.
    /// Simple, fast, and reliable detection strategy.
    /// </summary>
    public static PayloadType Detect(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return PayloadType.Unknown;

        var trimmed = payload.Trim();
        if (trimmed.Length == 0)
            return PayloadType.Unknown;

        var firstChar = trimmed[0];

        // JSON: starts with { or [
        if (firstChar == '{' || firstChar == '[')
            return PayloadType.Json;

        // XML: starts with <
        if (firstChar == '<')
            return PayloadType.Xml;

        return PayloadType.Unknown;
    }
}

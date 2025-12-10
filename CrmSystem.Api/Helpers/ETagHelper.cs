namespace CrmSystem.Api.Helpers;

/// <summary>
/// Helper class for generating and parsing ETags based on UpdatedAt timestamps
/// </summary>
public static class ETagHelper
{
    /// <summary>
    /// Generates a weak ETag from a DateTimeOffset timestamp
    /// </summary>
    /// <param name="updatedAt">The UpdatedAt timestamp</param>
    /// <returns>A weak ETag in the format W/"unix_milliseconds"</returns>
    public static string GenerateETag(DateTimeOffset updatedAt)
    {
        var milliseconds = updatedAt.ToUnixTimeMilliseconds();
        return $"W/\"{milliseconds}\"";
    }
    
    /// <summary>
    /// Parses an ETag string to extract the DateTimeOffset timestamp
    /// </summary>
    /// <param name="etag">The ETag string to parse</param>
    /// <returns>The DateTimeOffset if parsing succeeds, null otherwise</returns>
    public static DateTimeOffset? ParseETag(string? etag)
    {
        if (string.IsNullOrWhiteSpace(etag))
            return null;
        
        // Remove W/" and " from the ETag
        var value = etag.Replace("W/\"", "").Replace("\"", "");
        
        if (long.TryParse(value, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
        
        return null;
    }
}

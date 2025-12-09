using CrmSystem.Api.Helpers;
using Xunit;

namespace CrmSystem.Tests.UnitTests;

public class ETagHelperTests
{
    [Fact]
    public void GenerateETag_ShouldReturnWeakETagFormat()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 12, 8, 10, 30, 0, TimeSpan.Zero);
        var expectedMilliseconds = timestamp.ToUnixTimeMilliseconds();
        var expectedETag = $"W/\"{expectedMilliseconds}\"";

        // Act
        var result = ETagHelper.GenerateETag(timestamp);

        // Assert
        Assert.Equal(expectedETag, result);
    }

    [Fact]
    public void ParseETag_WithValidETag_ShouldReturnDateTimeOffset()
    {
        // Arrange
        var originalTimestamp = new DateTimeOffset(2024, 12, 8, 10, 30, 0, TimeSpan.Zero);
        var etag = ETagHelper.GenerateETag(originalTimestamp);

        // Act
        var result = ETagHelper.ParseETag(etag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalTimestamp.ToUnixTimeMilliseconds(), result.Value.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void ParseETag_WithNullETag_ShouldReturnNull()
    {
        // Act
        var result = ETagHelper.ParseETag(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseETag_WithEmptyETag_ShouldReturnNull()
    {
        // Act
        var result = ETagHelper.ParseETag("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseETag_WithWhitespaceETag_ShouldReturnNull()
    {
        // Act
        var result = ETagHelper.ParseETag("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseETag_WithInvalidFormat_ShouldReturnNull()
    {
        // Act
        var result = ETagHelper.ParseETag("invalid-etag");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseETag_WithoutWeakPrefix_ShouldStillParse()
    {
        // Arrange
        var milliseconds = 1702034400123L;
        var etag = $"\"{milliseconds}\"";

        // Act
        var result = ETagHelper.ParseETag(etag);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milliseconds, result.Value.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTimestamp()
    {
        // Arrange
        var originalTimestamp = DateTimeOffset.UtcNow;

        // Act
        var etag = ETagHelper.GenerateETag(originalTimestamp);
        var parsedTimestamp = ETagHelper.ParseETag(etag);

        // Assert
        Assert.NotNull(parsedTimestamp);
        // Compare at millisecond precision (ETag uses milliseconds)
        Assert.Equal(
            originalTimestamp.ToUnixTimeMilliseconds(),
            parsedTimestamp.Value.ToUnixTimeMilliseconds()
        );
    }
}

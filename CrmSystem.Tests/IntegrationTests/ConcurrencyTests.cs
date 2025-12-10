using System.Net;
using System.Net.Http.Json;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Models;
using CrmSystem.Api.Helpers;
using Xunit;

namespace CrmSystem.Tests.IntegrationTests;

/// <summary>
/// Integration tests for concurrency conflict scenarios
/// Tests Requirements: 10.1-10.6
/// </summary>
[Collection("Integration")]
public class ConcurrencyTests : IntegrationTestBase
{
    public ConcurrencyTests(CrmApiFactory factory) : base(factory)
    {
    }

    #region Customer Concurrency Tests (Requirement 10.3, 10.4)

    [Fact]
    public async Task UpdateCustomer_WithStaleETag_Returns409()
    {
        // Arrange
        var (customer, originalEtag) = await CreateCustomerAsync();
        
        // First update to change the ETag
        var firstUpdate = new UpdateCustomerRequest
        {
            CompanyName = "First Update",
            ContactName = customer.ContactName,
            Status = customer.Status
        };
        
        var firstRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{customer.Id}")
        {
            Content = JsonContent.Create(firstUpdate, options: JsonOptions)
        };
        firstRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        await Client.SendAsync(firstRequest);

        // Act - Try to update with the original (now stale) ETag
        var secondUpdate = new UpdateCustomerRequest
        {
            CompanyName = "Second Update",
            ContactName = customer.ContactName,
            Status = customer.Status
        };
        
        var secondRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{customer.Id}")
        {
            Content = JsonContent.Create(secondUpdate, options: JsonOptions)
        };
        secondRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        var response = await Client.SendAsync(secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.NotEmpty(apiResponse.Errors);
    }

    [Fact]
    public async Task UpdateCustomer_WithCurrentETag_Succeeds()
    {
        // Arrange
        var (customer, etag) = await CreateCustomerAsync();
        
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Company",
            ContactName = customer.ContactName,
            Status = customer.Status
        };
        
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{customer.Id}")
        {
            Content = JsonContent.Create(updateRequest, options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_WithoutIfMatchHeader_StillSucceeds()
    {
        // Arrange - Requirement 10.6: Should execute but log warning
        var (customer, _) = await CreateCustomerAsync();
        
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Without ETag",
            ContactName = customer.ContactName,
            Status = customer.Status
        };

        // Act - No If-Match header
        var response = await Client.PutAsJsonAsync($"/api/customers/{customer.Id}", updateRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion


    #region Customer Delete Concurrency Tests (Requirement 10.5)

    [Fact]
    public async Task DeleteCustomer_WithStaleETag_Returns409()
    {
        // Arrange
        var (customer, originalEtag) = await CreateCustomerAsync();
        
        // Update to change the ETag
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Company",
            ContactName = customer.ContactName,
            Status = customer.Status
        };
        
        var updateHttpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{customer.Id}")
        {
            Content = JsonContent.Create(updateRequest, options: JsonOptions)
        };
        updateHttpRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        await Client.SendAsync(updateHttpRequest);

        // Act - Try to delete with the original (now stale) ETag
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customer.Id}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        var response = await Client.SendAsync(deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        // Verify customer was NOT deleted
        var getResponse = await Client.GetAsync($"/api/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_WithCurrentETag_Succeeds()
    {
        // Arrange
        var (customer, etag) = await CreateCustomerAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customer.Id}");
        request.Headers.TryAddWithoutValidation("If-Match", etag);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region Interaction Concurrency Tests (Requirement 10.3, 10.4)

    [Fact]
    public async Task UpdateInteraction_WithStaleETag_Returns409()
    {
        // Arrange
        var (customer, _) = await CreateCustomerAsync();
        var (interaction, originalEtag) = await CreateInteractionAsync(customer.Id);
        
        // First update to change the ETag
        var firstUpdate = new UpdateInteractionRequest
        {
            Title = "First Update",
            Channel = interaction.Channel,
            HappenedAt = interaction.HappenedAt
        };
        
        var firstRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/interactions/{interaction.Id}")
        {
            Content = JsonContent.Create(firstUpdate, options: JsonOptions)
        };
        firstRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        await Client.SendAsync(firstRequest);

        // Act - Try to update with the original (now stale) ETag
        var secondUpdate = new UpdateInteractionRequest
        {
            Title = "Second Update",
            Channel = interaction.Channel,
            HappenedAt = interaction.HappenedAt
        };
        
        var secondRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/interactions/{interaction.Id}")
        {
            Content = JsonContent.Create(secondUpdate, options: JsonOptions)
        };
        secondRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        var response = await Client.SendAsync(secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInteraction_WithStaleETag_Returns409()
    {
        // Arrange
        var (customer, _) = await CreateCustomerAsync();
        var (interaction, originalEtag) = await CreateInteractionAsync(customer.Id);
        
        // Update to change the ETag
        var updateRequest = new UpdateInteractionRequest
        {
            Title = "Updated Title",
            Channel = interaction.Channel,
            HappenedAt = interaction.HappenedAt
        };
        
        var updateHttpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/interactions/{interaction.Id}")
        {
            Content = JsonContent.Create(updateRequest, options: JsonOptions)
        };
        updateHttpRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        await Client.SendAsync(updateHttpRequest);

        // Act - Try to delete with the original (now stale) ETag
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/interactions/{interaction.Id}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", originalEtag);
        var response = await Client.SendAsync(deleteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        // Verify interaction was NOT deleted
        var getResponse = await Client.GetAsync($"/api/interactions/{interaction.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion

    #region ETag Format Tests (Requirement 10.2)

    [Fact]
    public async Task CreateCustomer_ReturnsValidETagFormat()
    {
        // Arrange
        var request = CreateTestCustomerRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.True(response.Headers.Contains("ETag"));
        var etag = response.Headers.ETag?.Tag;
        Assert.NotNull(etag);
        
        // ETag should be in format W/"<milliseconds>"
        Assert.StartsWith("W/\"", etag);
        Assert.EndsWith("\"", etag);
        
        // Should be parseable
        var parsed = ETagHelper.ParseETag(etag);
        Assert.NotNull(parsed);
    }

    [Fact]
    public async Task GetCustomer_ReturnsValidETagFormat()
    {
        // Arrange
        var (customer, _) = await CreateCustomerAsync();

        // Act
        var response = await Client.GetAsync($"/api/customers/{customer.Id}");

        // Assert
        Assert.True(response.Headers.Contains("ETag"));
        var etag = response.Headers.ETag?.Tag;
        Assert.NotNull(etag);
        
        var parsed = ETagHelper.ParseETag(etag);
        Assert.NotNull(parsed);
    }

    #endregion
}

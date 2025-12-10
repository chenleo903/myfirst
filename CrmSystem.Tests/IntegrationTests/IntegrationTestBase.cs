using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using CrmSystem.Api.Data;
using Xunit;

namespace CrmSystem.Tests.IntegrationTests;

/// <summary>
/// Base class for integration tests providing common utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CrmApiFactory>
{
    protected readonly CrmApiFactory Factory;
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(CrmApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Creates a test customer with default values
    /// </summary>
    protected CreateCustomerRequest CreateTestCustomerRequest(
        string? companyName = null,
        string? contactName = null,
        CustomerStatus status = CustomerStatus.Lead)
    {
        return new CreateCustomerRequest
        {
            CompanyName = companyName ?? $"Test Company {Guid.NewGuid():N}",
            ContactName = contactName ?? $"Test Contact {Guid.NewGuid():N}",
            Status = status,
            Email = "test@example.com",
            Phone = "1234567890",
            Industry = "Technology",
            Source = CustomerSource.Website,
            Score = 50
        };
    }

    /// <summary>
    /// Creates a test interaction request with default values
    /// </summary>
    protected CreateInteractionRequest CreateTestInteractionRequest(
        string? title = null,
        InteractionChannel channel = InteractionChannel.Email)
    {
        return new CreateInteractionRequest
        {
            Title = title ?? $"Test Interaction {Guid.NewGuid():N}",
            Channel = channel,
            HappenedAt = DateTimeOffset.UtcNow,
            Summary = "Test summary"
        };
    }


    /// <summary>
    /// Creates a customer via API and returns the response
    /// </summary>
    protected async Task<(CustomerResponse Customer, string ETag)> CreateCustomerAsync(
        CreateCustomerRequest? request = null)
    {
        request ??= CreateTestCustomerRequest();
        
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        var etag = response.Headers.ETag?.Tag ?? string.Empty;
        
        return (apiResponse!.Data!, etag);
    }

    /// <summary>
    /// Creates an interaction via API and returns the response
    /// </summary>
    protected async Task<(InteractionResponse Interaction, string ETag)> CreateInteractionAsync(
        Guid customerId,
        CreateInteractionRequest? request = null)
    {
        request ??= CreateTestInteractionRequest();
        
        var response = await Client.PostAsJsonAsync(
            $"/api/customers/{customerId}/interactions", 
            request, 
            JsonOptions);
        response.EnsureSuccessStatusCode();
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<InteractionResponse>>(JsonOptions);
        var etag = response.Headers.ETag?.Tag ?? string.Empty;
        
        return (apiResponse!.Data!, etag);
    }

    /// <summary>
    /// Gets a fresh database context for direct database operations
    /// </summary>
    protected CrmDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    }

    /// <summary>
    /// Cleans up test data from the database
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        using var context = GetDbContext();
        context.Interactions.RemoveRange(context.Interactions);
        context.Customers.RemoveRange(context.Customers);
        await context.SaveChangesAsync();
    }
}

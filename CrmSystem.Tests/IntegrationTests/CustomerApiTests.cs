using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Models;
using Xunit;

namespace CrmSystem.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Customer API endpoints
/// Tests Requirements: 1.1-1.8, 2.1-2.9, 4.1, 6.1-6.9
/// </summary>
[Collection("Integration")]
public class CustomerApiTests : IntegrationTestBase
{
    public CustomerApiTests(CrmApiFactory factory) : base(factory)
    {
    }

    #region Create Customer Tests (Requirement 1.1, 1.7)

    [Fact]
    public async Task CreateCustomer_WithValidData_Returns201AndCustomer()
    {
        // Arrange
        var request = CreateTestCustomerRequest();

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.True(response.Headers.Contains("ETag"));

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(request.CompanyName, apiResponse.Data.CompanyName);
        Assert.Equal(request.ContactName, apiResponse.Data.ContactName);
        Assert.NotEqual(Guid.Empty, apiResponse.Data.Id);
    }

    [Fact]
    public async Task CreateCustomer_WithMissingCompanyName_Returns400()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            CompanyName = "", // Empty required field
            ContactName = "Test Contact"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.NotEmpty(apiResponse.Errors);
    }

    [Fact]
    public async Task CreateCustomer_WithMissingContactName_Returns400()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            CompanyName = "Test Company",
            ContactName = "" // Empty required field
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_Returns400()
    {
        // Arrange
        var request = CreateTestCustomerRequest();
        request.Email = "invalid-email";

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidScore_Returns400()
    {
        // Arrange
        var request = CreateTestCustomerRequest();
        request.Score = 150; // Out of range (0-100)

        // Act
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Get Customer Tests (Requirement 1.2)

    [Fact]
    public async Task GetCustomer_WithValidId_Returns200AndCustomer()
    {
        // Arrange
        var (customer, _) = await CreateCustomerAsync();

        // Act
        var response = await Client.GetAsync($"/api/customers/{customer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal(customer.Id, apiResponse.Data!.Id);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_Returns404()
    {
        // Act
        var response = await Client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Update Customer Tests (Requirement 1.3)

    [Fact]
    public async Task UpdateCustomer_WithValidData_Returns200AndUpdatedCustomer()
    {
        // Arrange
        var (customer, etag) = await CreateCustomerAsync();
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Company",
            ContactName = customer.ContactName,
            Status = CustomerStatus.Contacted,
            Score = 75
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

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal("Updated Company", apiResponse.Data!.CompanyName);
        Assert.Equal(CustomerStatus.Contacted, apiResponse.Data.Status);
        Assert.Equal(75, apiResponse.Data.Score);
    }

    [Fact]
    public async Task UpdateCustomer_WithNonExistentId_Returns404()
    {
        // Arrange
        var updateRequest = new UpdateCustomerRequest
        {
            CompanyName = "Updated Company",
            ContactName = "Updated Contact",
            Status = CustomerStatus.Lead
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", updateRequest, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion


    #region Delete Customer Tests (Requirement 1.4, 1.5, 1.6, 1.8)

    [Fact]
    public async Task DeleteCustomer_WithValidId_Returns204()
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

    [Fact]
    public async Task DeleteCustomer_ThenGetCustomer_Returns404()
    {
        // Arrange
        var (customer, etag) = await CreateCustomerAsync();

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customer.Id}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        await Client.SendAsync(deleteRequest);

        // Act
        var response = await Client.GetAsync($"/api/customers/{customer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_WithNonExistentId_Returns404()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_PreservesInteractions()
    {
        // Arrange - Create customer with interaction
        var (customer, _) = await CreateCustomerAsync();
        var (interaction, _) = await CreateInteractionAsync(customer.Id);

        // Get the customer's ETag for deletion
        var getResponse = await Client.GetAsync($"/api/customers/{customer.Id}");
        var etag = getResponse.Headers.ETag?.Tag ?? string.Empty;

        // Act - Delete customer
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customer.Id}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        await Client.SendAsync(deleteRequest);

        // Assert - Interaction still exists in database (soft delete preserves data)
        using var context = GetDbContext();
        var dbInteraction = await context.Interactions.FindAsync(interaction.Id);
        Assert.NotNull(dbInteraction);
    }

    #endregion

    #region List Customers Tests (Requirement 2.1-2.9)

    [Fact]
    public async Task GetCustomers_WithDefaultParams_ReturnsPagedResponse()
    {
        // Arrange
        await CreateCustomerAsync();
        await CreateCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CustomerResponse>>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.Items);
        Assert.True(apiResponse.Data.Total >= 2);
    }

    [Fact]
    public async Task GetCustomers_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        await CreateCustomerAsync(CreateTestCustomerRequest(status: CustomerStatus.Lead));
        await CreateCustomerAsync(CreateTestCustomerRequest(status: CustomerStatus.Won));

        // Act
        var response = await Client.GetAsync("/api/customers?status=Won");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CustomerResponse>>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.All(apiResponse.Data!.Items, c => Assert.Equal(CustomerStatus.Won, c.Status));
    }

    [Fact]
    public async Task GetCustomers_WithKeywordSearch_ReturnsMatchingResults()
    {
        // Arrange
        var uniqueKeyword = $"UniqueCompany{Guid.NewGuid():N}";
        await CreateCustomerAsync(CreateTestCustomerRequest(companyName: uniqueKeyword));
        await CreateCustomerAsync(CreateTestCustomerRequest(companyName: "OtherCompany"));

        // Act
        var response = await Client.GetAsync($"/api/customers?keyword={uniqueKeyword}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CustomerResponse>>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.Single(apiResponse.Data!.Items);
        Assert.Contains(uniqueKeyword, apiResponse.Data.Items[0].CompanyName);
    }

    [Fact]
    public async Task GetCustomers_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create multiple customers
        for (int i = 0; i < 5; i++)
        {
            await CreateCustomerAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/customers?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CustomerResponse>>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.Equal(2, apiResponse.Data!.Items.Count);
        Assert.True(apiResponse.Data.Total >= 5);
    }

    [Fact]
    public async Task GetCustomers_WithInvalidPageSize_Returns400()
    {
        // Act
        var response = await Client.GetAsync("/api/customers?pageSize=200");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomers_ExcludesDeletedCustomers()
    {
        // Arrange
        var (customer, etag) = await CreateCustomerAsync();
        var customerId = customer.Id;

        // Delete the customer
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customerId}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        await Client.SendAsync(deleteRequest);

        // Act
        var response = await Client.GetAsync("/api/customers");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CustomerResponse>>>(JsonOptions);
        Assert.DoesNotContain(apiResponse!.Data!.Items, c => c.Id == customerId);
    }

    #endregion


    #region Uniqueness Constraint Tests (Requirement 4.1)

    [Fact]
    public async Task CreateCustomer_WithDuplicateCompanyAndContact_Returns409()
    {
        // Arrange
        var request = CreateTestCustomerRequest();
        await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Act - Try to create duplicate
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.NotEmpty(apiResponse.Errors);
    }

    [Fact]
    public async Task CreateCustomer_AfterDeletingDuplicate_Succeeds()
    {
        // Arrange - Create and delete a customer
        var request = CreateTestCustomerRequest();
        var createResponse = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);
        var apiResponse = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        var etag = createResponse.Headers.ETag?.Tag ?? string.Empty;

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{apiResponse!.Data!.Id}");
        deleteRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        await Client.SendAsync(deleteRequest);

        // Act - Create customer with same company/contact name
        var response = await Client.PostAsJsonAsync("/api/customers", request, JsonOptions);

        // Assert - Should succeed because original was soft-deleted
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion

    #region Response Format Tests (Requirement 6.1, 6.2)

    [Fact]
    public async Task SuccessResponse_HasCorrectFormat()
    {
        // Arrange
        var (customer, _) = await CreateCustomerAsync();

        // Act
        var response = await Client.GetAsync($"/api/customers/{customer.Id}");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Empty(apiResponse.Errors);
    }

    [Fact]
    public async Task ErrorResponse_HasCorrectFormat()
    {
        // Act
        var response = await Client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        // Assert
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>(JsonOptions);
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Null(apiResponse.Data);
        Assert.NotEmpty(apiResponse.Errors);
    }

    #endregion
}

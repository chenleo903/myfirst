using Microsoft.AspNetCore.Mvc;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Helpers;
using CrmSystem.Api.Models;
using CrmSystem.Api.Services;

namespace CrmSystem.Api.Controllers;

/// <summary>
/// 客户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// 获取客户列表（支持筛选、搜索、分页）
    /// GET /api/customers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<CustomerResponse>>>> GetCustomers(
        [FromQuery] CustomerSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting customers list with filters: {@Request}", request);

        var result = await _customerService.SearchCustomersAsync(request, cancellationToken);

        var response = new PagedResponse<CustomerResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            Total = result.Total
        };

        return Ok(new ApiResponse<PagedResponse<CustomerResponse>>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }


    /// <summary>
    /// 获取客户详情
    /// GET /api/customers/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetCustomer(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting customer details for ID: {CustomerId}", id);

        var customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
        var response = MapToResponse(customer);

        // Set ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(customer.UpdatedAt);

        return Ok(new ApiResponse<CustomerResponse>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }

    /// <summary>
    /// 创建客户
    /// POST /api/customers
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new customer: {CompanyName} - {ContactName}", 
            request.CompanyName, request.ContactName);

        var customer = await _customerService.CreateCustomerAsync(request, cancellationToken);
        var response = MapToResponse(customer);

        // Set ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(customer.UpdatedAt);

        // Return 201 Created with Location header
        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            new ApiResponse<CustomerResponse>
            {
                Success = true,
                Data = response,
                Errors = new List<ErrorDetail>()
            });
    }

    /// <summary>
    /// 更新客户
    /// PUT /api/customers/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> UpdateCustomer(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating customer: {CustomerId}", id);

        // Parse If-Match header for optimistic concurrency control
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var originalUpdatedAt = ETagHelper.ParseETag(ifMatch);

        var customer = await _customerService.UpdateCustomerAsync(id, request, originalUpdatedAt, cancellationToken);
        var response = MapToResponse(customer);

        // Set new ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(customer.UpdatedAt);

        return Ok(new ApiResponse<CustomerResponse>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }

    /// <summary>
    /// 软删除客户
    /// DELETE /api/customers/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting customer: {CustomerId}", id);

        // Parse If-Match header for optimistic concurrency control
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var originalUpdatedAt = ETagHelper.ParseETag(ifMatch);

        await _customerService.DeleteCustomerAsync(id, originalUpdatedAt, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Map Customer entity to CustomerResponse DTO
    /// </summary>
    private static CustomerResponse MapToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            CompanyName = customer.CompanyName,
            ContactName = customer.ContactName,
            Wechat = customer.Wechat,
            Phone = customer.Phone,
            Email = customer.Email,
            Industry = customer.Industry,
            Source = customer.Source,
            Status = customer.Status,
            Tags = customer.Tags,
            Score = customer.Score,
            LastInteractionAt = customer.LastInteractionAt,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}

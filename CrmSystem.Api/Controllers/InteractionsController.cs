using Microsoft.AspNetCore.Mvc;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Helpers;
using CrmSystem.Api.Models;
using CrmSystem.Api.Services;

namespace CrmSystem.Api.Controllers;

/// <summary>
/// 互动记录管理控制器
/// </summary>
[ApiController]
[Route("api")]
public class InteractionsController : ControllerBase
{
    private readonly IInteractionService _interactionService;
    private readonly ILogger<InteractionsController> _logger;

    public InteractionsController(
        IInteractionService interactionService,
        ILogger<InteractionsController> logger)
    {
        _interactionService = interactionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取客户的互动记录列表（时间线）
    /// GET /api/customers/{customerId}/interactions
    /// </summary>
    [HttpGet("customers/{customerId:guid}/interactions")]
    public async Task<ActionResult<ApiResponse<List<InteractionResponse>>>> GetInteractions(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting interactions for customer: {CustomerId}", customerId);

        var interactions = await _interactionService.GetInteractionsByCustomerIdAsync(customerId, cancellationToken);
        var response = interactions.Select(MapToResponse).ToList();

        return Ok(new ApiResponse<List<InteractionResponse>>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }

    /// <summary>
    /// 创建互动记录
    /// POST /api/customers/{customerId}/interactions
    /// </summary>
    [HttpPost("customers/{customerId:guid}/interactions")]
    public async Task<ActionResult<ApiResponse<InteractionResponse>>> CreateInteraction(
        Guid customerId,
        [FromBody] CreateInteractionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating interaction for customer: {CustomerId}, Title: {Title}", 
            customerId, request.Title);

        var interaction = await _interactionService.CreateInteractionAsync(customerId, request, cancellationToken);
        var response = MapToResponse(interaction);

        // Set ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(interaction.UpdatedAt);

        // Return 201 Created with Location header
        return CreatedAtAction(
            nameof(GetInteraction),
            new { id = interaction.Id },
            new ApiResponse<InteractionResponse>
            {
                Success = true,
                Data = response,
                Errors = new List<ErrorDetail>()
            });
    }


    /// <summary>
    /// 获取单个互动记录
    /// GET /api/interactions/{id}
    /// </summary>
    [HttpGet("interactions/{id:guid}")]
    public async Task<ActionResult<ApiResponse<InteractionResponse>>> GetInteraction(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting interaction: {InteractionId}", id);

        var interaction = await _interactionService.GetInteractionByIdAsync(id, cancellationToken);
        var response = MapToResponse(interaction);

        // Set ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(interaction.UpdatedAt);

        return Ok(new ApiResponse<InteractionResponse>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }

    /// <summary>
    /// 更新互动记录
    /// PUT /api/interactions/{id}
    /// </summary>
    [HttpPut("interactions/{id:guid}")]
    public async Task<ActionResult<ApiResponse<InteractionResponse>>> UpdateInteraction(
        Guid id,
        [FromBody] UpdateInteractionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating interaction: {InteractionId}", id);

        // Parse If-Match header for optimistic concurrency control
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var originalUpdatedAt = ETagHelper.ParseETag(ifMatch);

        var interaction = await _interactionService.UpdateInteractionAsync(id, request, originalUpdatedAt, cancellationToken);
        var response = MapToResponse(interaction);

        // Set new ETag header
        Response.Headers["ETag"] = ETagHelper.GenerateETag(interaction.UpdatedAt);

        return Ok(new ApiResponse<InteractionResponse>
        {
            Success = true,
            Data = response,
            Errors = new List<ErrorDetail>()
        });
    }

    /// <summary>
    /// 删除互动记录
    /// DELETE /api/interactions/{id}
    /// </summary>
    [HttpDelete("interactions/{id:guid}")]
    public async Task<IActionResult> DeleteInteraction(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting interaction: {InteractionId}", id);

        // Parse If-Match header for optimistic concurrency control
        var ifMatch = Request.Headers["If-Match"].FirstOrDefault();
        var originalUpdatedAt = ETagHelper.ParseETag(ifMatch);

        await _interactionService.DeleteInteractionAsync(id, originalUpdatedAt, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Map Interaction entity to InteractionResponse DTO
    /// </summary>
    private static InteractionResponse MapToResponse(Interaction interaction)
    {
        return new InteractionResponse
        {
            Id = interaction.Id,
            CustomerId = interaction.CustomerId,
            HappenedAt = interaction.HappenedAt,
            Channel = interaction.Channel,
            Stage = interaction.Stage,
            Title = interaction.Title,
            Summary = interaction.Summary,
            RawContent = interaction.RawContent,
            NextAction = interaction.NextAction,
            Attachments = interaction.Attachments,
            CreatedAt = interaction.CreatedAt,
            UpdatedAt = interaction.UpdatedAt
        };
    }
}

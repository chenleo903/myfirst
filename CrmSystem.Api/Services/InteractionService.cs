using Microsoft.EntityFrameworkCore;
using CrmSystem.Api.Data;
using CrmSystem.Api.DTOs;
using CrmSystem.Api.Exceptions;
using CrmSystem.Api.Models;
using CrmSystem.Api.Repositories;

namespace CrmSystem.Api.Services;

/// <summary>
/// 互动记录业务逻辑实现
/// </summary>
public class InteractionService : IInteractionService
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly CrmDbContext _context;
    private readonly ILogger<InteractionService> _logger;

    public InteractionService(
        IInteractionRepository interactionRepository,
        ICustomerRepository customerRepository,
        CrmDbContext context,
        ILogger<InteractionService> logger)
    {
        _interactionRepository = interactionRepository;
        _customerRepository = customerRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<Interaction> CreateInteractionAsync(
        Guid customerId,
        CreateInteractionRequest request,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // 使用事务确保互动记录创建和客户 LastInteractionAt 更新的原子性
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 验证客户存在且未删除
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken);

                if (customer == null)
                {
                    throw new NotFoundException("Customer not found");
                }

                // 创建互动记录
                var interaction = new Interaction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    HappenedAt = request.HappenedAt.ToUniversalTime(),
                    Channel = request.Channel,
                    Stage = request.Stage,
                    Title = request.Title,
                    Summary = request.Summary,
                    RawContent = request.RawContent,
                    NextAction = request.NextAction,
                    Attachments = request.Attachments,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _context.Interactions.Add(interaction);

                // 更新客户的 LastInteractionAt
                customer.LastInteractionAt = interaction.HappenedAt;
                customer.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return interaction;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<List<Interaction>> GetInteractionsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        // 验证客户存在且未删除
        var customerExists = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customerExists == null)
        {
            throw new NotFoundException("Customer not found");
        }

        return await _interactionRepository.GetByCustomerIdAsync(customerId, cancellationToken);
    }

    public async Task<Interaction> GetInteractionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var interaction = await _interactionRepository.GetByIdAsync(id, cancellationToken);

        if (interaction == null)
        {
            throw new NotFoundException("Interaction not found");
        }

        return interaction;
    }

    public async Task<Interaction> UpdateInteractionAsync(
        Guid id,
        UpdateInteractionRequest request,
        DateTimeOffset? originalUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        // 获取现有互动记录（使用跟踪查询以便更新）
        var interaction = await _context.Interactions
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (interaction == null)
        {
            throw new NotFoundException("Interaction not found");
        }

        // 验证版本（如果提供了 If-Match）
        if (originalUpdatedAt.HasValue)
        {
            var currentMillis = interaction.UpdatedAt.ToUnixTimeMilliseconds();
            var providedMillis = originalUpdatedAt.Value.ToUnixTimeMilliseconds();

            if (currentMillis != providedMillis)
            {
                throw new ConcurrencyException("Interaction has been modified by another user", interaction.UpdatedAt);
            }
        }
        else
        {
            _logger.LogWarning("Update request without If-Match header for interaction {InteractionId}", id);
        }

        // 更新字段
        interaction.HappenedAt = request.HappenedAt.ToUniversalTime();
        interaction.Channel = request.Channel;
        interaction.Stage = request.Stage;
        interaction.Title = request.Title;
        interaction.Summary = request.Summary;
        interaction.RawContent = request.RawContent;
        interaction.NextAction = request.NextAction;
        interaction.Attachments = request.Attachments;
        interaction.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return interaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            // EF Core 检测到并发冲突
            var current = await _context.Interactions
                .AsNoTracking()
                .FirstAsync(i => i.Id == id, cancellationToken);
            throw new ConcurrencyException("Interaction has been modified by another user", current.UpdatedAt);
        }
    }

    public async Task DeleteInteractionAsync(
        Guid id,
        DateTimeOffset? originalUpdatedAt = null,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            // 使用事务确保互动记录删除和客户 LastInteractionAt 重新计算的原子性
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var interaction = await _context.Interactions
                    .Include(i => i.Customer)
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

                if (interaction == null)
                {
                    throw new NotFoundException("Interaction not found");
                }

                // 验证版本（如果提供了 If-Match）
                if (originalUpdatedAt.HasValue)
                {
                    var currentMillis = interaction.UpdatedAt.ToUnixTimeMilliseconds();
                    var providedMillis = originalUpdatedAt.Value.ToUnixTimeMilliseconds();

                    if (currentMillis != providedMillis)
                    {
                        throw new ConcurrencyException("Interaction has been modified by another user", interaction.UpdatedAt);
                    }
                }
                else
                {
                    _logger.LogWarning("Delete request without If-Match header for interaction {InteractionId}", id);
                }

                var customerId = interaction.CustomerId;

                // 删除互动记录
                _context.Interactions.Remove(interaction);

                // 重新计算客户的 LastInteractionAt
                var latestInteraction = await _context.Interactions
                    .Where(i => i.CustomerId == customerId && i.Id != id)
                    .OrderByDescending(i => i.HappenedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var customer = interaction.Customer;
                customer.LastInteractionAt = latestInteraction?.HappenedAt;
                customer.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                var current = await _context.Interactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
                
                if (current != null)
                {
                    throw new ConcurrencyException("Interaction has been modified by another user", current.UpdatedAt);
                }
                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}

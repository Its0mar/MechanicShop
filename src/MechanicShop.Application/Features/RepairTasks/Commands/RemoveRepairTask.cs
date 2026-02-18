using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands;

public sealed record RemoveRepairTaskCommand(Guid RepairTaskId)
    : IRequest<Result<Deleted>>;

public class RemoveRepairTaskCommandValidator : AbstractValidator<RemoveRepairTaskCommand>
{
    public RemoveRepairTaskCommandValidator()
    {
        RuleFor(x => x.RepairTaskId)
            .NotEmpty().WithMessage("Repair task Id is required.");
    }
}

public class RemoveRepairTaskCommandHandler(
    ILogger<RemoveRepairTaskCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache
    )
    : IRequestHandler<RemoveRepairTaskCommand, Result<Deleted>>
{
    private readonly ILogger<RemoveRepairTaskCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(RemoveRepairTaskCommand command, CancellationToken ct)
    {
        var repairTask = await _context.RepairTasks
            .FindAsync([command.RepairTaskId], ct);

        if (repairTask is null)
        {
            _logger.LogWarning("RepairTask {RepairTaskId} not found for deletion.", command.RepairTaskId);
            return ApplicationErrors.RepairTaskNotFound;
        }

        var isInUse = await _context.WorkOrders.AsNoTracking()
            .SelectMany(x => x.RepairTasks)
            .AnyAsync(rt => rt.Id == command.RepairTaskId, ct);

        if (isInUse)
        {
            _logger.LogWarning("RepairTask {RepairTaskId} cannot be deleted — in use by work orders.", command.RepairTaskId);

            return RepairTaskErrors.InUse;
        }

        _context.RepairTasks.Remove(repairTask);
        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("repair-task", ct);

        _logger.LogInformation("RepairTask {RepairTaskId} deleted successfully.", command.RepairTaskId);

        return Result.Deleted;
    }
}
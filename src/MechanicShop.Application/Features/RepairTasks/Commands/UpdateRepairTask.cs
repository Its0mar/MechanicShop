using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.RepairTasks.Parts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands;

public sealed record UpdateRepairTaskCommand(
    Guid RepairTaskId,
    string Name,
    decimal LaborCost,
    RepairDurationInMinutes EstimatedDurationInMins,
    List<UpdateRepairTaskPartCommand> Parts
) : IRequest<Result<Updated>>;

public class UpdateRepairTaskCommandValidator : AbstractValidator<UpdateRepairTaskCommand>
{
    public UpdateRepairTaskCommandValidator()
    {
        RuleFor(x => x.RepairTaskId)
            .NotEmpty().WithMessage("Repair task ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Task name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LaborCost)
            .InclusiveBetween(1, 10_000)
            .WithMessage("Labor cost must be between 1 and 10,000.");

        RuleFor(x => x.EstimatedDurationInMins)
            .IsInEnum()
            .WithMessage("Invalid duration selected.");

        RuleFor(x => x.Parts)
            .NotNull()
            .Must(p => p.Count > 0)
            .WithMessage("At least one part is required.");

        RuleForEach(x => x.Parts)
            .SetValidator(new UpdateRepairTaskPartCommandValidator());
    }
}

public class UpdateRepairTaskCommandHandler(IAppDbContext context,
                                            ILogger<UpdateRepairTaskCommandHandler> logger,
                                            HybridCache cache) : IRequestHandler<UpdateRepairTaskCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<UpdateRepairTaskCommandHandler> _logger = logger;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Updated>> Handle(UpdateRepairTaskCommand command, CancellationToken ct)
    {
        var repairTask = await _context.RepairTasks.Include(rt => rt.Parts)
            .FirstOrDefaultAsync(rt => rt.Id == command.RepairTaskId, ct); 

        if (repairTask is null)
        {
            logger.LogWarning("RepairTask {RepairTaskId} not found for update.", command.RepairTaskId);

            return ApplicationErrors.RepairTaskNotFound;
        }

        var validatedParts = new List<Part>();

        foreach (var p in command.Parts)
        {
            var partId = p.PartId ?? Guid.NewGuid();

            var partResult = Part.Create(partId, p.Name, p.Cost, p.Quantity);

            if (partResult.IsError)
            {
                return partResult.Errors ?? [];
            }

            validatedParts.Add(partResult.Value);
        }

        var updateRepairTaskResult = repairTask.Update(command.Name, command.LaborCost, command.EstimatedDurationInMins);

        if (updateRepairTaskResult.IsError)
        {
            return updateRepairTaskResult.Errors ?? [];
        }

        var upsertPartsResult = repairTask.UpsertParts(validatedParts);

        if (upsertPartsResult.IsError)
        {
            return upsertPartsResult.Errors ?? [];
        }

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveByTagAsync("repair-task", ct);

        return Result.Updated;    
    }
}
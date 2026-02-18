using FluentValidation;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.RepairTasks.Parts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands;

public sealed record CreateRepairTaskCommand(
    string? Name,
    decimal LaborCost,
    RepairDurationInMinutes? EstimatedDurationInMins,
    List<CreateRepairTaskPartCommand> Parts
) : IRequest<Result<RepairTaskDto>>;

public sealed class CreateRepairTaskCommandValidator : AbstractValidator<CreateRepairTaskCommand>
{
    public CreateRepairTaskCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LaborCost)
            .GreaterThan(0).WithMessage("Labor cost must be greater than 0.");

        RuleFor(x => x.EstimatedDurationInMins)
            .NotNull().WithMessage("Estimated duration is required.")
            .IsInEnum();

        RuleFor(x => x.Parts)
            .NotNull().WithMessage("Parts list cannot be null.")
            .Must(p => p.Count > 0).WithMessage("At least one part is required.");

        RuleForEach(x => x.Parts).SetValidator(new CreateRepairTaskPartCommandValidator());
    }
}

public sealed class CreateRepairTaskCommandHandler(IAppDbContext context,
                                                   ILogger<CreateRepairTaskCommandHandler> logger,
                                                   HybridCache hybridCache) : IRequestHandler<CreateRepairTaskCommand, Result<RepairTaskDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<CreateRepairTaskCommandHandler> _logger = logger;
    private readonly HybridCache _cache = hybridCache;

    public async Task<Result<RepairTaskDto>> Handle(CreateRepairTaskCommand command, CancellationToken ct)
    {
        var nameExist = await _context.RepairTasks.AnyAsync(rt => EF.Functions.Like(rt.Name, command.Name),ct);

        if (nameExist)
        {
            _logger.LogWarning("Duplicate part name '{PartName}'.", command.Name);

            return RepairTaskErrors.DuplicateName;
        }

        List<Part> parts = [];
        foreach (var p in command.Parts)
        {
            var partResult = Part.Create(Guid.NewGuid(), p.Name, p.Cost, p.Quantity);
            if (partResult.IsError)
            {
                return partResult.Errors ?? [];
            }

            parts.Add(partResult.Value);
        }
        
         var createRepairTaskResult = RepairTask.Create(
                    id: Guid.NewGuid(),
                    name: command.Name!,
                    laborCost: command.LaborCost,
                    estimatedDurationInMins: command.EstimatedDurationInMins!.Value,
                    parts: parts);

        if (createRepairTaskResult.IsError)
        {
            return createRepairTaskResult.Errors ?? [];
        }

        var repairTask = createRepairTaskResult.Value;
        _context.RepairTasks.Add(repairTask);

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveByTagAsync("repair-task", ct);

        return repairTask.ToDto();
    }
}
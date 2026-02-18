using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Queries;

public sealed record GetRepairTaskByIdQuery(Guid RepairTaskId) : ICachedQuery<Result<RepairTaskDto>>
{
    public string CacheKey => "repair-task";
    public string[] Tags => ["repair-task"];
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}

public sealed class GetRepairTaskByIdQueryValidator : AbstractValidator<GetRepairTaskByIdQuery>
{
    public GetRepairTaskByIdQueryValidator()
    {
        RuleFor(query => query.RepairTaskId)
            .NotEmpty()
            .WithErrorCode("RepairTaskId_Is_Required")
            .WithMessage("RepairTaskId is required.");
    }
}

public sealed class GetRepairTaskByIdQueryHandler(IAppDbContext context,
                                                  ILogger<GetRepairTaskByIdQueryHandler> logger)
                                                : IRequestHandler<GetRepairTaskByIdQuery, Result<RepairTaskDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<GetRepairTaskByIdQueryHandler> _logger = logger;

    public async Task<Result<RepairTaskDto>> Handle(GetRepairTaskByIdQuery query, CancellationToken ct)
    {
        var repairTask = await _context.RepairTasks.AsNoTracking()
            .Include(rt => rt.Parts)
            .FirstOrDefaultAsync(rt => rt.Id == query.RepairTaskId, ct);

        if (repairTask is null)
        {
            _logger.LogWarning("Repair task with id {RepairTaskId} was not found", query.RepairTaskId);
            return ApplicationErrors.RepairTaskNotFound;
        }

        return repairTask.ToDto();            
    }
}
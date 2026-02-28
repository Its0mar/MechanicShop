using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands;

public sealed record AssignLaborCommand(Guid WorkOrderId, Guid LaborId) : IRequest<Result<Updated>>;

public sealed class AssignLaborCommandValidator : AbstractValidator<AssignLaborCommand>
{
    public AssignLaborCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
         .NotEmpty()
         .WithErrorCode("WorkOrderId_Required")
         .WithMessage("WorkOrderId is required.");

        RuleFor(x => x.LaborId)
           .NotEmpty()
           .WithErrorCode("LaborId_Required")
           .WithMessage("LaborId is required.");
    }
}

public class AssignLaborCommandHandler(
    ILogger<AssignLaborCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IWorkOrderPolicy WorkOrderRuleService
    )
    : IRequestHandler<AssignLaborCommand, Result<Updated>>
{
    private readonly ILogger<AssignLaborCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;
    private readonly IWorkOrderPolicy _workOrderValidator = WorkOrderRuleService;

    public async Task<Result<Updated>> Handle(AssignLaborCommand command, CancellationToken ct)
    {
        var workOrder = await _context.WorkOrders
            .FirstOrDefaultAsync(a => a.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrderNotFound;
        }

        var labor = await _context.Employees.FindAsync([command.LaborId], ct);

        if (labor is null)
        {
            _logger.LogError("Invalid LaborId: {LaborId}", command.LaborId);
            return ApplicationErrors.LaborNotFound;
        }

        if (await _workOrderValidator.IsLaborOccupied(command.LaborId, command.WorkOrderId, workOrder.StartAtUtc, workOrder.EndAtUtc))
        {
            _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", workOrder.LaborId);
            return ApplicationErrors.LaborOccupied;
        }

        var updateLaborResult = workOrder.UpdateLabor(command.LaborId);

        if (updateLaborResult.IsError)
        {
            foreach (var error in updateLaborResult.Errors!)
            {
                _logger.LogError("[LaborUpdate] {ErrorCode}: {ErrorDescription}", error.Code, error.Description);
            }

            return updateLaborResult.Errors;
        }

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Updated;
    }
}
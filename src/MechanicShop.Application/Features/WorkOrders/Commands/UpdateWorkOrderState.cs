using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.WorkOrders;
using MechanicShop.Domain.WorkOrders.Enums;
using MechanicShop.Domain.WorkOrders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands;

public sealed record UpdateWorkOrderStateCommand(Guid WorkOrderId, WorkOrderState State) : IRequest<Result<Updated>>;

public sealed class UpdateWorkOrderStateCommandValidator : AbstractValidator<UpdateWorkOrderStateCommand>
{
    public UpdateWorkOrderStateCommandValidator()
    {
        RuleFor(x => x.State)
           .IsInEnum()
           .WithErrorCode("WorkOrderStatus_Invalid")
           .WithMessage("Status must be a valid WorkOrderStatus value.");
    }
}

public sealed class UpdateWorkOrderStateCommandHanlder(IAppDbContext context,
                                                       ILogger<UpdateWorkOrderStateCommandHanlder> logger,
                                                       HybridCache cache,
                                                       TimeProvider timeProvider) : IRequestHandler<UpdateWorkOrderStateCommand, Result<Updated>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<UpdateWorkOrderStateCommandHanlder> _logger = logger;
    private readonly HybridCache _cache = cache;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Result<Updated>> Handle(UpdateWorkOrderStateCommand command, CancellationToken ct)
    {
        var workOrder = await _context.WorkOrders.FirstOrDefaultAsync(wo => wo.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);

            return ApplicationErrors.WorkOrderNotFound;
        }

        if (workOrder.StartAtUtc > _timeProvider.GetUtcNow())
        {
            _logger.LogError("State transition for WorkOrder Id '{WorkOrderId}` is not allowed before the work order�s scheduled start time.", command.WorkOrderId);

            return WorkOrderErrors.StateTransitionNotAllowed(workOrder.StartAtUtc);
        }

        var updateStatusResult = workOrder.UpdateState(command.State);

        if (updateStatusResult.IsError)
        {
            _logger.LogError("Failed to update status: {Error}", updateStatusResult.TopError.Description);

            return updateStatusResult.Errors ?? [];
        }

        if (command.State == WorkOrderState.Completed)
        {
            workOrder.AddDomainEvent(new WorkOrderCompleted { WorkOrderId = command.WorkOrderId });
        }

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Updated;
    }
}
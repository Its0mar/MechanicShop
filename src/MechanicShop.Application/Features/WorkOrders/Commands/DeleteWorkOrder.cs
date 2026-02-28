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

public sealed record DeleteWorkOrderCommand(Guid WorkOrderId) : IRequest<Result<Deleted>>;

public sealed class DeleteWorkOrderCommandValidator : AbstractValidator<DeleteWorkOrderCommand>
{
    public DeleteWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
           .NotEmpty()
           .WithErrorCode("WorkOrderId_Required")
           .WithMessage("WorkOrderId is required.");
    }
}

public sealed class DeleteWorkOrderCommandHandler(IAppDbContext context,
                                                  ILogger<DeleteWorkOrderCommandHandler> logger,
                                                  HybridCache cache) : IRequestHandler<DeleteWorkOrderCommand, Result<Deleted>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<DeleteWorkOrderCommandHandler> _logger = logger;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Deleted>> Handle(DeleteWorkOrderCommand command, CancellationToken ct)
    {
        var workOrder = await _context.WorkOrders.FirstOrDefaultAsync(wo => wo.Id == command.WorkOrderId, ct);
                if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);

            return ApplicationErrors.WorkOrderNotFound;
        }

        if (workOrder.State is not WorkOrderState.Scheduled)
        {
            _logger.LogError(
                "Deletion failed: only 'Scheduled' WorkOrders can be deleted. Current status: {Status}",
                workOrder.State);

            return WorkOrderErrors.Readonly;
        }

        _context.WorkOrders.Remove(workOrder);

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Deleted;
    }
}
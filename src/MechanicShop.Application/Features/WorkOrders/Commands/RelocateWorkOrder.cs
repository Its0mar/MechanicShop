using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.WorkOrders.Enums;
using MechanicShop.Domain.WorkOrders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands;

public sealed record RelocateWorkOrderCommand(Guid WorkOrderId,
                                              Spot NewSpot,
                                              DateTimeOffset NewStartAt) : IRequest<Result<Updated>>;

public sealed class RelocateWorkOrderCommandValidator : AbstractValidator<RelocateWorkOrderCommand>
{
    public RelocateWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty();

        RuleFor(x => x.NewStartAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("New start time must be in the future.");

        RuleFor(x => x.NewSpot)
            .IsInEnum();
    }
}

public class RelocateWorkOrderCommandHandler(
    ILogger<RelocateWorkOrderCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IWorkOrderPolicy WorkOrderValidator
    )
    : IRequestHandler<RelocateWorkOrderCommand, Result<Updated>>
{
    private readonly ILogger<RelocateWorkOrderCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;
    private readonly IWorkOrderPolicy _appointmentValidator = WorkOrderValidator;

    public async Task<Result<Updated>> Handle(RelocateWorkOrderCommand command, CancellationToken ct)
    {
        var workOrder = await _context.WorkOrders.Include(a => a.RepairTasks)
                                                 .Include(a => a.Labor)
                                                 .Include(a => a.Vehicle)
                                                 .FirstOrDefaultAsync(wo => wo.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            _logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);

            return ApplicationErrors.WorkOrderNotFound;
        }

         var duration = workOrder.EndAtUtc.Subtract(workOrder.StartAtUtc).Duration();

        var endAt = command.NewStartAt.Add(duration);

        var checkSpotAvailabilityResult = await _appointmentValidator.CheckSpotAvailabilityAsync(
            workOrder.Spot,
            command.NewStartAt,
            endAt,
            excludeWorkOrderId: workOrder.Id,
            ct);

        if (checkSpotAvailabilityResult.IsError)
        {
            _logger.LogError("Spot: {Spot} is not available.", workOrder.Spot.ToString());

            return checkSpotAvailabilityResult.Errors ?? [];
        }

        if (await _appointmentValidator.IsLaborOccupied(workOrder.LaborId, command.WorkOrderId, command.NewStartAt, endAt))
        {
            _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", workOrder.LaborId);

            return ApplicationErrors.LaborOccupied;
        }

        if (await _appointmentValidator.IsVehicleAlreadyScheduled(workOrder.VehicleId, command.NewStartAt, endAt, command.WorkOrderId))
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", workOrder.VehicleId);

            return ApplicationErrors.VehicleSchedulingConflict;
        }

        var updateTimingResult = workOrder.UpdateTiming(command.NewStartAt, endAt);

        if (updateTimingResult.IsError)
        {
            _logger.LogError("Failed to update timing: {Error}", updateTimingResult.TopError.Description);

            return updateTimingResult.Errors ?? [];
        }

        var updateSpotResult = workOrder.UpdateSpot(command.NewSpot);

        if (updateTimingResult.IsError)
        {
            _logger.LogError("Failed to update Spot: {Error}", updateSpotResult.TopError.Description);

            return updateTimingResult.Errors ?? [];
        }

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _context.SaveChangesAsync(ct);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _cache.RemoveByTagAsync("work-order", ct);

        return Result.Updated;
    }
}
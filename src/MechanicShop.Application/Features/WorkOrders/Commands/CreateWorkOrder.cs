using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.WorkOrders;
using MechanicShop.Domain.WorkOrders.Enums;
using MechanicShop.Domain.WorkOrders.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands;

public sealed record CreateWorkOrderCommand(
    Spot Spot,
    Guid VehicleId,
    DateTimeOffset StartAt,
    List<Guid> RepairTaskIds,
    Guid? LaborId)
: IRequest<Result<WorkOrderDto>>;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(request => request.VehicleId)
            .NotEmpty()
            .WithMessage("VehicleId is required.");

        RuleFor(request => request.StartAt)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("StartAt must be in the future.");

        RuleFor(request => request.RepairTaskIds)
            .NotEmpty()
            .WithMessage("At least one repair task must be selected");

        RuleFor(request => request.LaborId)
            .Must(laborId => laborId is null || laborId != Guid.Empty)
            .WithMessage("If provided, LaborId must not be empty.");

        RuleFor(x => x.Spot)
          .IsInEnum()
          .WithErrorCode("Spot_Invalid")
          .WithMessage("Spot must be a valid Spot value. [A, B, C, D]");
    }
}

public sealed class CreateWorkOrderCommandHanlder(IAppDbContext context,
                                                  ILogger<CreateWorkOrderCommandHanlder> logger,
                                                  IWorkOrderPolicy workOrderPolicy,
                                                  HybridCache cache) : IRequestHandler<CreateWorkOrderCommand, Result<WorkOrderDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<CreateWorkOrderCommandHanlder> _logger = logger;
    private readonly IWorkOrderPolicy _workOrderPolicy = workOrderPolicy;
    private readonly HybridCache _cache = cache;

    public async Task<Result<WorkOrderDto>> Handle(CreateWorkOrderCommand command, CancellationToken ct)
    {
        var repairTasks = await _context.RepairTasks.Where(rt => command.RepairTaskIds.Contains(rt.Id)).ToListAsync(ct);

        if (repairTasks.Count != command.RepairTaskIds.Count)
        {
            var missingIds = command.RepairTaskIds.Except(repairTasks.Select(rt => rt.Id)).ToArray();
            _logger.LogError("Some RepairTaskIds not found: {MissingIds}", string.Join(", ", missingIds));

            return ApplicationErrors.RepairTaskNotFound;
        }

        var totalEstimatedDuration = TimeSpan.FromMinutes(repairTasks.Sum(rt => (int) rt.EstimatedDurationInMins));
        var endAt = command.StartAt.Add(totalEstimatedDuration);

        if (_workOrderPolicy.IsOutsideOperatingHours(command.StartAt, totalEstimatedDuration))
        {
            _logger.LogError("The WorkOrder time ({StartAt} ? {EndAt}) is outside of store operating hours.", command.StartAt, endAt);

            return ApplicationErrors.WorkOrderOutsideOperatingHour(command.StartAt, endAt);
        }

        var checkMinRequirementResult = _workOrderPolicy.ValidateMinimumRequirement(command.StartAt, endAt);

        if (checkMinRequirementResult.IsError)
        {
            _logger.LogError("WorkOrder duration is shorter than the configured minimum.");

            return checkMinRequirementResult.Errors ?? [];
        }

        var checkSpotAvailabilityResult = await _workOrderPolicy.CheckSpotAvailabilityAsync(
            command.Spot,
            command.StartAt,
            endAt,
            excludeWorkOrderId: null,
            ct);

        if (checkSpotAvailabilityResult.IsError)
        {
            _logger.LogError("Spot: {Spot} is not available.", command.Spot.ToString());
            return checkSpotAvailabilityResult.Errors ?? [];
        }

        var vehicle = await _context.Vehicles.Include(v => v.Customer).FirstOrDefaultAsync(v => v.Id == command.VehicleId, cancellationToken: ct);

        if (vehicle is null)
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' does not exist.", command.VehicleId);

            return ApplicationErrors.VehicleNotFound;
        }

        var labor = await _context.Employees.FindAsync([command.LaborId], ct);

        if (labor is null)
        {
            _logger.LogError("Invalid LaborId: {LaborId}", command.LaborId.ToString());
            return ApplicationErrors.LaborNotFound;
        }

        var hasVehicleConflict = await _context.WorkOrders
            .AnyAsync(
                a =>
                a.VehicleId == command.VehicleId &&
                a.StartAtUtc.Date == command.StartAt.Date &&
                a.StartAtUtc < endAt &&
                a.EndAtUtc > command.StartAt,
                ct);

        if (hasVehicleConflict)
        {
            _logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", command.VehicleId);
            return Error.Conflict(
                code: "Vehicle_Overlapping_WorkOrders",
                description: "The vehicle already has an overlapping WorkOrder.");
        }

        var isLaborOccupied = await _context.WorkOrders
            .AnyAsync(
                a =>
                a.LaborId == command.LaborId &&
                a.StartAtUtc < endAt &&
                a.EndAtUtc > command.StartAt,
                ct);

        if (isLaborOccupied)
        {
            _logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", command.LaborId);
            return Error.Conflict(
                code: "Labor_Occupied",
                description: "Labor is already occupied during the requested time.");
        }

        var createWorkOrderResult = WorkOrder.Create(
            Guid.NewGuid(),
            command.VehicleId,
            command.StartAt,
            endAt,
            command.LaborId!.Value,
            command.Spot,
            repairTasks);

        if (createWorkOrderResult.IsError)
        {
            _logger.LogError("Failed to create WorkOrder: {Error}", createWorkOrderResult.TopError.Description);

            return createWorkOrderResult.Errors ?? [];
        }

        var workOrder = createWorkOrderResult.Value;

        _context.WorkOrders.Add(workOrder);

        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        await _context.SaveChangesAsync(ct);

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;

        _logger.LogInformation("WorkOrder with Id '{WorkOrderId}' created successfully.", workOrder.Id);

        await _cache.RemoveByTagAsync("work-order", ct);

        return workOrder.ToDto();
    }
}
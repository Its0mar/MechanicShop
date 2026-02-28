using MechanicShop.Application.Features.Labors;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.WorkOrders.Enums;

namespace MechanicShop.Application.Features.Scheduling.Dtos;

public class AvailabilitySlotDto
{
    public Guid? WorkOrderId { get; set; }
    public Spot Spot { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Vehicle { get; set; }
    public LaborDto? Labor { get; set; }
    public bool IsOccupied { get; set; }
    public bool? IsAvailable { get; set; }
    public bool WorkOrderLocked { get; set; }
    public WorkOrderState? State { get; set; }
    public RepairTaskDto[]? RepairTasks { get; set; }
}
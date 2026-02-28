using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Application.Features.Labors;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.WorkOrders;

namespace MechanicShop.Application.Features.WorkOrders.Mappers;

public static class WorkOrderMapper
{
    public static WorkOrderDto ToDto(this WorkOrder entity)
    {
        return new WorkOrderDto
        {
            WorkOrderId = entity.Id,
            Spot = entity.Spot,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            Labor = entity.Labor?.ToDto(),
            RepairTasks = entity.RepairTasks.ToDtos(),
            Vehicle = entity.Vehicle?.ToDto(),
            State = entity.State,
            TotalPartCost = entity.TotalPartsCost,
            TotalLaborCost = entity.TotalLaborCost,
            TotalCost = entity.Total,
            TotalDurationInMins = entity.RepairTasks.Sum(rt => (int)rt.EstimatedDurationInMins),
            InvoiceId = entity.Invoice?.Id,
            CreatedAt = entity.CreatedAtUtc
        };
    }

    public static List<WorkOrderDto> ToDtos(this IEnumerable<WorkOrder> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    public static WorkOrderListItemDto ToListItemDto(this WorkOrder entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new WorkOrderListItemDto
        {
            WorkOrderId = entity.Id,
            Spot = entity.Spot,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            Vehicle = entity.Vehicle!.ToDto(),
            Labor = entity.Labor is null ? null :
                $"{entity.Labor.FirstName} {entity.Labor.LastName}",
            State = entity.State,
            RepairTasks = entity.RepairTasks.Select(rt => rt.Name).ToList()
        };
    }

}
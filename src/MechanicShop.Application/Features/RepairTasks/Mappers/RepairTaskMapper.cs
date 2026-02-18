using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Parts;

namespace MechanicShop.Application.Features.RepairTasks.Mappers;

public static class RepairTaskMapper
{
    public static RepairTaskDto ToDto(this RepairTask entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RepairTaskDto
        {
            RepairTaskId = entity.Id,
            Name = entity.Name,
            EstimatedDurationInMins = entity.EstimatedDurationInMins,
            LaborCost = entity.LaborCost,
            TotalCost = entity.TotalCost,
            Parts = entity.Parts.ToDtos()
        };
    }

    public static List<RepairTaskDto> ToDtos(this IEnumerable<RepairTask> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }


    public static PartDto ToDto(this Part entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PartDto
        {
            PartId = entity.Id,
            Name = entity.Name,
            Cost = entity.Cost,
            Quantity = entity.Quantity
        };
    }

    public static List<PartDto> ToDtos(this IEnumerable<Part> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

}
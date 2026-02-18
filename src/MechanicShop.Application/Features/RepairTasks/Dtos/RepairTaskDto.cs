using MechanicShop.Domain.RepairTasks.Enums;

namespace MechanicShop.Application.Features.RepairTasks.Dtos;

public class RepairTaskDto
{
    public Guid RepairTaskId {get; set;}
    public string Name {get; set;} = string.Empty;
    public RepairDurationInMinutes EstimatedDurationInMins {get; set;}
    public decimal LaborCost {get; set;}
    public decimal TotalCost {get; set;}
    public List<PartDto> Parts {get; set;} = [];
}
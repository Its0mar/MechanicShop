using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.RepairTasks.Parts;

public static class PartErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Part.Name.Required", "Part name is required.");

    public static readonly Error CostInvalid =
        Error.Validation("Part.Cost.Invalid", "Part cost must be between 1 and 10,000.");

    public static readonly Error QuantityInvalid =
        Error.Validation("Part.Quantity.Invalid", "Quantity must be between 1 and 10.");
}
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.RepairTasks;

public static class RepairTaskErrors
{
    public static Error NameRequired =>
        Error.Validation("RepairTask.Name.Required", "Name is required.");

    public static Error LaborCostInvalid =>
        Error.Validation("RepairTask.LaborCost.Invalid", "Labor cost must be between 1 and 10,000.");

    public static Error DurationInvalid =>
        Error.Validation("RepairTask.Duration.Invalid", "Invalid duration selected.");

    public static Error PartsRequired =>
        Error.Validation("RepairTask.Parts.Required", "At least one part is required.");

    public static Error PartNameRequired =>
        Error.Validation("RepairTask.Parts.Name.Required", "All parts must have a name.");

    public static Error AtLeastOneRepairTaskIsRequired =>
        Error.Validation("RepairTask.Required", "At least one repair task must be specified.");

    public static Error InUse =>
        Error.Conflict("RepairTask.InUse", "Cannot delete a repair task that is used in work orders.");

    public static Error DuplicateName =>
        Error.Conflict("RepairTaskPart.Duplicate", "A part with the same name already exists in this repair task.");
}
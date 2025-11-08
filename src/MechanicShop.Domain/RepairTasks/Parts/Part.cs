using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.RepairTasks.Parts;

public sealed class Part : AuditableEntity
{
    public string Name { get; private set; }
    public decimal Cost { get; private set; }
    public int Quantity { get; private set; }

#pragma warning disable CS8618
    private Part()
    { }

    private Part(Guid id, string name, decimal cost, int quantity) : base(id)
    {
        Name = name;
        Cost = cost;
        Quantity = quantity;
    }
#pragma warning restore CS8618

    public static Result<Part> Create(Guid id, string name, decimal cost, int quantity)
    {
        var validationResult = Validate(name, cost, quantity);
        if (validationResult is not null) return validationResult;


        return new Part(id, name.Trim(), cost, quantity);
    }

    public Result<Updated> Update(string name, decimal cost, int quantity)
    {
        var validationResult = Validate(name, cost, quantity);
        if (validationResult is not null) return validationResult;

        Name = name.Trim();
        Cost = cost;
        Quantity = quantity;

        return Result.Updated;
    }

    private static Error? Validate(string name, decimal cost, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return PartErrors.NameRequired;
        }

        if (cost <= 0 || cost > 10000)
        {
            return PartErrors.CostInvalid;
        }

        if (quantity <= 0 || cost > 10)
        {
            return PartErrors.QuantityInvalid;
        }

        return null;
    }

}
using FluentValidation;

namespace MechanicShop.Application.Features.RepairTasks.Commands;

public sealed record UpdateRepairTaskPartCommand(
    Guid? PartId,
    string Name,
    decimal Cost,
    int Quantity
);

public class UpdateRepairTaskPartCommandValidator : AbstractValidator<UpdateRepairTaskPartCommand>
{
    public UpdateRepairTaskPartCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Cost)
            .InclusiveBetween(1, 10_000)
            .WithMessage("Cost must be between 1 and 10,000.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 10)
            .WithMessage("Quantity must be between 1 and 10.");
    }
}
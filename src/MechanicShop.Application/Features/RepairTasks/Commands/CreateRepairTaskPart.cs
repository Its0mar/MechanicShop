using FluentValidation;
using MechanicShop.Domain.Common.Results;
using MediatR;

namespace MechanicShop.Application.Features.RepairTasks.Commands;

public sealed record CreateRepairTaskPartCommand(string Name,
                                                 decimal Cost,
                                                 int Quantity) :IRequest<Result<Success>>;


public sealed class CreateRepairTaskPartCommandValidator : AbstractValidator<CreateRepairTaskPartCommand>
{
    public CreateRepairTaskPartCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Cost)
            .GreaterThan(0).WithMessage("Part cost must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}                                                 
using FluentValidation;
using MechanicShop.Domain.Common.Results;
using MediatR;

namespace MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

public record GenerateTokenQuery(string Email,
                                 string Password) : IRequest<Result<TokenResponse>>;


public sealed class GenerateTokenQueryValidator : AbstractValidator<GenerateTokenQuery>
{
    public GenerateTokenQueryValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("Email_Null_Or_Empty")
            .WithMessage("Email cannot be null or empty");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithErrorCode("Paasword_Null_Or_Empty")
            .WithMessage("Password cannot be null or empty");
    
    }
}
                                 

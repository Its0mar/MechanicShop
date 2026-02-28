using FluentValidation;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Mappers;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Queries;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : ICachedQuery<Result<InvoiceDto>>
{
    public string CacheKey => $"invoice_{InvoiceId}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => ["invoice"];
}

public sealed class GetInvoiceByIdQueryValidator : AbstractValidator<GetInvoiceByIdQuery>
{
    public GetInvoiceByIdQueryValidator()
    {
        RuleFor(request => request.InvoiceId)
            .NotEmpty()
            .WithErrorCode("InvoiceId_Is_Required")
            .WithMessage("InvoiceId is required.");
    }
}

public class GetInvoiceByIdQueryHandler(
    ILogger<GetInvoiceByIdQueryHandler> logger,
    IAppDbContext context
    )
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery query, CancellationToken ct)
    {
        var invoice = await context.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .Include(i => i.WorkOrder!)
                .ThenInclude(w => w.Vehicle!)
                    .ThenInclude(v => v.Customer)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, ct);

        if (invoice is null)
        {
            logger.LogWarning("Invoice not found. InvoiceId: {InvoiceId}", query.InvoiceId);
            return Error.NotFound("Invoice not found.");
        }

        return invoice.ToDto();
    }
}
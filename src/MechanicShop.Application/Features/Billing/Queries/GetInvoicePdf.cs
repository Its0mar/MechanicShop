using FluentValidation;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Queries;

public sealed record GetInvoicePdfQuery(Guid InvoiceId) : IRequest<Result<InvoicePdfDto>>;

public sealed class GetInvoicePdfQueryValidator : AbstractValidator<GetInvoicePdfQuery>
{
    public GetInvoicePdfQueryValidator()
    {
        RuleFor(request => request.InvoiceId)
            .NotEmpty()
            .WithErrorCode("InvoiceId_Is_Required")
            .WithMessage("InvoiceId is required.");
    }
}

public class GetInvoicePdfQureyHandler(
    ILogger<GetInvoicePdfQureyHandler> logger,
    IInvoicePdfGenerator pdfGenerator,
    IAppDbContext context
    )
    : IRequestHandler<GetInvoicePdfQuery, Result<InvoicePdfDto>>
{
    public async Task<Result<InvoicePdfDto>> Handle(GetInvoicePdfQuery query, CancellationToken ct)
    {
        var invoice = await context.Invoices.AsNoTracking()
           .Include(i => i.LineItems)
           .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, ct);

        if (invoice is null)
        {
            logger.LogWarning("Invoice not found. InvoiceId: {InvoiceId}", query.InvoiceId);
            return Error.NotFound("Invoice not found.");
        }

        try
        {
            var pdfBytes = pdfGenerator.Generate(invoice);

            var invoicePdf = new InvoicePdfDto
            {
                Content = pdfBytes,
                FileName = $"invoice-{invoice.Id}.pdf"
            };

            return invoicePdf;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PDF for InvoiceId: {InvoiceId}", query.InvoiceId);
            return Error.Failure("An error occurred while generating the invoice PDF.");
        }
    }
}
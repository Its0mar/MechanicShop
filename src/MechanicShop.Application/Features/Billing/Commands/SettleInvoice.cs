using FluentValidation;
using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Commands;

public sealed record SettleInvoiceCommand(Guid InvoiceId) : IRequest<Result<Success>>;

public sealed class SettleInvoiceCommandValidator : AbstractValidator<SettleInvoiceCommand>
{
    public SettleInvoiceCommandValidator()
    {
        RuleFor(request => request.InvoiceId)
            .NotEmpty()
            .WithErrorCode("InvoiceId_Is_Required")
            .WithMessage("InvoiceId is required.");
    }
}

public class SettleInvoiceCommandHandler(
    ILogger<SettleInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    TimeProvider datetime
    )
    : IRequestHandler<SettleInvoiceCommand, Result<Success>>
{
    private readonly ILogger<SettleInvoiceCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _datetime = datetime;
    private readonly HybridCache _cache = cache;

    public async Task<Result<Success>> Handle(SettleInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await _context.Invoices
              .FirstOrDefaultAsync(w => w.Id == command.InvoiceId, ct);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found.", command.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        var payInvoiceResult = invoice.MarkAsPaid(_datetime);

        if (payInvoiceResult.IsError)
        {
            _logger.LogWarning(
               "Invoice payment failed for InvoiceId: {InvoiceId}. Errors: {Errors}",
               invoice.Id,
               payInvoiceResult.Errors);

            return payInvoiceResult.Errors ?? [];
        }

        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByTagAsync("invoice", ct);

        _logger.LogInformation("Invoice {InvoiceId} successfully paid.", invoice.Id);

        return Result.Success;
    }
}
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.WorkOrders.Billing;
using MechanicShop.Application.Features.Customers.Mappers;

namespace MechanicShop.Application.Features.Billing.Mappers;

public static class InvoiceMapper
{
    public static InvoiceDto ToDto(this Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new InvoiceDto
        {
            InvoiceId = invoice.Id,
            WorkOrderId = invoice.WorkOrderId,
            Customer = invoice.WorkOrder!.Vehicle!.Customer!.ToDto(),
            Vehicle = invoice.WorkOrder.Vehicle.ToDto(),
            IssuedAtUtc = invoice.IssuedAtUtc,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            Total = invoice.Total,
            PaymentStatus = invoice.Status.ToString(),
            Items = invoice.LineItems.Select(x => x.ToDto()).ToList()
        };
    }

    public static List<InvoiceDto> ToDtos(this IEnumerable<Invoice> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    public static InvoiceLineItemDto ToDto(this InvoiceLineItem item)
    {
        return new InvoiceLineItemDto
        {
            InvoiceId = item.InvoiceId,
            LineNumber = item.LineNumber,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }

    public static List<InvoiceLineItemDto> ToDtos(this IEnumerable<InvoiceLineItem> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
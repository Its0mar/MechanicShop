using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.WorkOrders.Billing;

public sealed class Invoice : AuditableEntity
{
    public Guid WorkOrderId { get; }
    public DateTimeOffset IssuedAtUtc { get; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; }
    public decimal Subtotal => LineItems.Sum(x => x.LineTotal);
    public decimal Total => Subtotal - DiscountAmount + TaxAmount;

    public DateTimeOffset? PaidAt { get; private set; }

    public WorkOrder? WorkOrder { get; set; }

    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    public InvoiceStatus Status { get; private set; }

    private Invoice()
    { }

    private Invoice(
        Guid id,
        Guid workOrderId,
        DateTimeOffset issuedAt,
        List<InvoiceLineItem> lineItems,
        decimal discountAmount,
        decimal taxAmount)
        : base(id)
    {
        WorkOrderId = workOrderId;
        IssuedAtUtc = issuedAt;
        DiscountAmount = discountAmount;
        Status = InvoiceStatus.Unpaid;
        TaxAmount = taxAmount;
        _lineItems = lineItems;
    }

    public static Result<Invoice> Create(
        Guid id,
        Guid workOrderId,
        List<InvoiceLineItem> items,
        decimal discountAmount,
        decimal taxAmount,
        TimeProvider datetime)
    {
        if (workOrderId == Guid.Empty)
        {
            return InvoiceErrors.WorkOrderIdInvalid;
        }

        if (items is null || items.Count == 0)
        {
            return InvoiceErrors.LineItemsEmpty;
        }

        return new Invoice(id, workOrderId, datetime.GetUtcNow(), items, discountAmount, taxAmount);
    }

    public Result<Updated> ApplyDiscount(decimal discountAmount)
    {
        if (Status != InvoiceStatus.Unpaid)
        {
            return InvoiceErrors.InvoiceLocked;
        }

        if (discountAmount < 0)
        {
            return InvoiceErrors.DiscountNegative;
        }

        if (discountAmount > Subtotal)
        {
            return InvoiceErrors.DiscountExceedsSubtotal;
        }

        DiscountAmount = discountAmount;

        return Result.Updated;
    }

    public Result<Updated> MarkAsPaid(TimeProvider timeProvider)
    {
        if (Status != InvoiceStatus.Unpaid)
        {
            return InvoiceErrors.InvoiceLocked;
        }

        Status = InvoiceStatus.Paid;
        PaidAt = timeProvider.GetUtcNow();

        return Result.Updated;
    }
}
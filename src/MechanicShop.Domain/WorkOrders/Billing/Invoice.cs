using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.WorkOrders.Billing;

public sealed class Invoice : AuditableEntity
{
    public Guid WorkOrderId { get; }
    public DateTimeOffset IssuedAtUtc { get; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; }
    public decimal Subtotal => LineItems.Sum(line => line.LineTotal);
    public decimal Total => Subtotal - DiscountAmount + TaxAmount;
    public DateTimeOffset? PaidAt { get; private set; }

    public WorkOrder? WorkOrder { get; set; }
    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;
    public InvoiceStatus Status { get; private set; }


    private Invoice()
    { }

    private Invoice(Guid id, Guid workOrderId, DateTimeOffset issuedAt, List<InvoiceLineItem> lineItems,
        decimal discountAmount, decimal taxAmount) : base(id)
    {
        WorkOrderId = workOrderId;
        IssuedAtUtc = issuedAt;
        DiscountAmount = discountAmount;
        Status = InvoiceStatus.Unpaid;
        TaxAmount = taxAmount;
        _lineItems = lineItems;
    }

}
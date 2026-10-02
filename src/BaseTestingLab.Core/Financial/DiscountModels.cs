namespace BaseTestingLab.Core.Financial;

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public record DiscountRule
{
    public string Code { get; init; } = string.Empty;
    public DiscountType Type { get; init; }
    public decimal Value { get; init; }
    public decimal MinSubtotalRequired { get; init; } = 0m;
    public decimal MaxDiscountLimit { get; init; } = decimal.MaxValue;
}

public record InvoiceItem
{
    public string Sku { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public record InvoiceSummary
{
    public decimal GrossSubtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal NetSubtotal => GrossSubtotal - DiscountTotal;
    public decimal TaxRate { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal GrandTotal => NetSubtotal + TaxTotal;
    public int TotalItemsCount { get; init; }
}

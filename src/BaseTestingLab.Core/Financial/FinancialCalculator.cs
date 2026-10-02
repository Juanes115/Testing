using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.Financial;

public class FinancialCalculator
{
    private readonly decimal _defaultTaxRate;

    public FinancialCalculator(decimal defaultTaxRate = 0.19m)
    {
        if (defaultTaxRate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(defaultTaxRate), "La tasa de impuesto debe estar entre 0.0 y 1.0 (0% y 100%).");

        _defaultTaxRate = defaultTaxRate;
    }

    public InvoiceSummary CalculateInvoice(
        IEnumerable<InvoiceItem> items,
        DiscountRule? discountRule = null,
        decimal? customTaxRate = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return new InvoiceSummary
            {
                GrossSubtotal = 0m,
                DiscountTotal = 0m,
                TaxRate = customTaxRate ?? _defaultTaxRate,
                TaxTotal = 0m,
                TotalItemsCount = 0
            };
        }

        foreach (var item in itemList)
        {
            if (item.UnitPrice < 0m)
                throw new FinancialException($"El precio unitario del ítem '{item.Sku}' no puede ser negativo.", item.UnitPrice);

            if (item.Quantity <= 0)
                throw new FinancialException($"La cantidad del ítem '{item.Sku}' debe ser mayor a cero.", item.Quantity);
        }

        var grossSubtotal = itemList.Sum(i => i.LineTotal);
        var totalQuantity = itemList.Sum(i => i.Quantity);
        var discountAmount = CalculateDiscount(grossSubtotal, discountRule);

        var netSubtotal = grossSubtotal - discountAmount;
        var taxRate = customTaxRate ?? _defaultTaxRate;
        var taxAmount = RoundMoney(netSubtotal * taxRate);

        return new InvoiceSummary
        {
            GrossSubtotal = RoundMoney(grossSubtotal),
            DiscountTotal = RoundMoney(discountAmount),
            TaxRate = taxRate,
            TaxTotal = taxAmount,
            TotalItemsCount = totalQuantity
        };
    }

    public decimal CalculateDiscount(decimal grossAmount, DiscountRule? rule)
    {
        if (rule == null || grossAmount <= 0m || grossAmount < rule.MinSubtotalRequired)
            return 0m;

        decimal calculatedDiscount;

        switch (rule.Type)
        {
            case DiscountType.Percentage:
                if (rule.Value is < 0m or > 100m)
                    throw new FinancialException("El porcentaje de descuento debe estar entre 0 y 100.", rule.Value);

                calculatedDiscount = grossAmount * (rule.Value / 100m);
                break;

            case DiscountType.FixedAmount:
                if (rule.Value < 0m)
                    throw new FinancialException("El valor fijo de descuento no puede ser negativo.", rule.Value);

                calculatedDiscount = rule.Value;
                break;

            default:
                throw new NotSupportedException($"Tipo de descuento no soportado: {rule.Type}");
        }

        if (calculatedDiscount > rule.MaxDiscountLimit)
        {
            calculatedDiscount = rule.MaxDiscountLimit;
        }

        if (calculatedDiscount > grossAmount)
        {
            calculatedDiscount = grossAmount;
        }

        return RoundMoney(calculatedDiscount);
    }

    public static decimal RoundMoney(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.ToEven);
    }
}

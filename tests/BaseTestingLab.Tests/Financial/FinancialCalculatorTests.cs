using BaseTestingLab.Core.Common.Exceptions;
using BaseTestingLab.Core.Financial;
using FluentAssertions;
using Xunit;

namespace BaseTestingLab.Tests.Financial;

public class FinancialCalculatorTests
{
    private readonly FinancialCalculator _calculator = new(defaultTaxRate: 0.19m);

    [Fact]
    public void CalculateInvoice_WithMultipleItemsAndPercentageDiscount_ShouldComputeAccurateTotals()
    {
        var items = new List<InvoiceItem>
        {
            new() { Sku = "SKU-001", Description = "Teclado Mecánico", UnitPrice = 100.00m, Quantity = 2 },
            new() { Sku = "SKU-002", Description = "Mouse Gamer", UnitPrice = 50.00m, Quantity = 1 }
        };

        var discount = new DiscountRule
        {
            Code = "PROMO10",
            Type = DiscountType.Percentage,
            Value = 10m
        };

        var summary = _calculator.CalculateInvoice(items, discount);

        summary.GrossSubtotal.Should().Be(250.00m);
        summary.DiscountTotal.Should().Be(25.00m);
        summary.NetSubtotal.Should().Be(225.00m);
        summary.TaxRate.Should().Be(0.19m);
        summary.TaxTotal.Should().Be(42.75m);
        summary.GrandTotal.Should().Be(267.75m);
        summary.TotalItemsCount.Should().Be(3);
    }

    [Fact]
    public void CalculateInvoice_WhenSubtotalBelowMinimum_ShouldNotApplyDiscount()
    {
        var items = new List<InvoiceItem>
        {
            new() { Sku = "SKU-SMALL", Description = "Cable USB", UnitPrice = 15.00m, Quantity = 1 }
        };

        var discount = new DiscountRule
        {
            Code = "MIN100",
            Type = DiscountType.FixedAmount,
            Value = 10m,
            MinSubtotalRequired = 50.00m
        };

        var summary = _calculator.CalculateInvoice(items, discount);

        summary.DiscountTotal.Should().Be(0m);
        summary.NetSubtotal.Should().Be(15.00m);
    }

    [Fact]
    public void CalculateDiscount_WhenExceedsMaxCap_ShouldCapAtMaximumLimit()
    {
        var rule = new DiscountRule
        {
            Code = "MAXCAP",
            Type = DiscountType.Percentage,
            Value = 50m,
            MaxDiscountLimit = 100m
        };

        var discount = _calculator.CalculateDiscount(1000m, rule);
        discount.Should().Be(100m);
    }

    [Fact]
    public void CalculateInvoice_WhenItemHasNegativePrice_ShouldThrowFinancialException()
    {
        var invalidItems = new List<InvoiceItem>
        {
            new() { Sku = "SKU-ERR", Description = "Ítem Inválido", UnitPrice = -20m, Quantity = 1 }
        };

        Action act = () => _calculator.CalculateInvoice(invalidItems);

        act.Should().Throw<FinancialException>()
           .WithMessage("*no puede ser negativo*")
           .And.InvalidAmount.Should().Be(-20m);
    }

    [Theory]
    [InlineData(2.555, 2.56)]
    [InlineData(2.545, 2.54)]
    [InlineData(10.125, 10.12)]
    [InlineData(10.135, 10.14)]
    public void RoundMoney_ShouldFollowBankersRounding(decimal input, decimal expected)
    {
        FinancialCalculator.RoundMoney(input).Should().Be(expected);
    }

    [Fact]
    public void CalculateInvoice_WhenItemsListEmpty_ShouldReturnZeroSummary()
    {
        var summary = _calculator.CalculateInvoice(Enumerable.Empty<InvoiceItem>());

        summary.GrossSubtotal.Should().Be(0m);
        summary.GrandTotal.Should().Be(0m);
        summary.TotalItemsCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WhenTaxRateOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        Action act = () => new FinancialCalculator(-0.05m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateDiscount_WhenPercentageInvalid_ShouldThrowFinancialException()
    {
        var rule = new DiscountRule { Code = "BAD", Type = DiscountType.Percentage, Value = 150m };
        Action act = () => _calculator.CalculateDiscount(100m, rule);
        act.Should().Throw<FinancialException>().WithMessage("*entre 0 y 100*");
    }

    [Fact]
    public void CalculateDiscount_WhenFixedNegative_ShouldThrowFinancialException()
    {
        var rule = new DiscountRule { Code = "BAD", Type = DiscountType.FixedAmount, Value = -10m };
        Action act = () => _calculator.CalculateDiscount(100m, rule);
        act.Should().Throw<FinancialException>().WithMessage("*no puede ser negativo*");
    }
}

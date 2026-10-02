namespace BaseTestingLab.Core.Common.Exceptions;

public class FinancialException : Exception
{
    public decimal? InvalidAmount { get; }

    public FinancialException(string message, decimal? invalidAmount = null) : base(message)
    {
        InvalidAmount = invalidAmount;
    }
}

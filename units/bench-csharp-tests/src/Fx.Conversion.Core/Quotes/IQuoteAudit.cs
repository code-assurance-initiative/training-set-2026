namespace Fx.Conversion.Quotes;

/// <summary>Records every quote issued, for reconciliation with the trades booked against it.</summary>
public interface IQuoteAudit
{
    void Issued(Quote quote);
}

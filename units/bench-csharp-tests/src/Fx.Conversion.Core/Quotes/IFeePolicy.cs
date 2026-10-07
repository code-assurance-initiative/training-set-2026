using Fx.Conversion.Monetary;

namespace Fx.Conversion.Quotes;

/// <summary>What converting an amount costs, in the amount's own currency.</summary>
public interface IFeePolicy
{
    Money FeeFor(Money source);
}

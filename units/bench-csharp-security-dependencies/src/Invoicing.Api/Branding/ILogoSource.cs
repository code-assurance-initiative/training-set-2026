namespace Invoicing.Api.Branding;

public interface ILogoSource
{
    /// <summary>The tenant's logo image, or null when the tenant has none.</summary>
    Task<byte[]?> GetLogoAsync(string tenant, CancellationToken cancellationToken);
}

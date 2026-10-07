using System.Security.Cryptography;
using DocumentExport.Api.Tokens;
using Microsoft.Extensions.Options;

namespace DocumentExport.UnitTests.Tokens;

public sealed class DownloadTokenServiceTests
{
    private static readonly DownloadTokenOptions Settings = new()
    {
        Issuer = "https://exports.test",
        Audience = "document-export-downloads",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
        Lifetime = TimeSpan.FromMinutes(5),
    };

    [Fact]
    public async Task An_issued_token_validates_and_names_its_export()
    {
        var service = new DownloadTokenService(Options.Create(Settings), TimeProvider.System);
        var exportId = Guid.NewGuid();

        var issued = service.Issue(exportId);
        var identity = await service.ValidateAsync(issued.Token, TestContext.Current.CancellationToken);

        Assert.NotNull(identity);
        Assert.Equal(exportId.ToString("D"), identity.FindFirst(DownloadTokenService.ExportIdClaim)?.Value);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var anHourAgo = new ShiftedTimeProvider(TimeSpan.FromHours(-1));
        var issuer = new DownloadTokenService(Options.Create(Settings), anHourAgo);
        var validator = new DownloadTokenService(Options.Create(Settings), TimeProvider.System);

        var issued = issuer.Issue(Guid.NewGuid());

        Assert.Null(await validator.ValidateAsync(issued.Token, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        var other = new DownloadTokenOptions
        {
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
        };
        var forged = new DownloadTokenService(Options.Create(other), TimeProvider.System).Issue(Guid.NewGuid());
        var service = new DownloadTokenService(Options.Create(Settings), TimeProvider.System);

        Assert.Null(await service.ValidateAsync(forged.Token, TestContext.Current.CancellationToken));
    }

    private sealed class ShiftedTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow().Add(offset);
    }
}

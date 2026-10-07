using System.ComponentModel.DataAnnotations;

namespace ReportDesk.Api.Controllers;

public sealed record ExportRequest
{
    [Required]
    [StringLength(20)]
    public string Format { get; init; } = string.Empty;

    [StringLength(2048)]
    public string? ReturnUrl { get; init; }
}

public sealed record FeedPullRequest
{
    [Required]
    [StringLength(2048)]
    public string FeedUrl { get; init; } = string.Empty;
}

public sealed record WebhookTestRequest
{
    [Required]
    public Uri? CallbackUrl { get; init; }
}

public sealed record DeliveryRequest
{
    [Required]
    public Guid ReportId { get; init; }

    [Required]
    public Guid SubscriberId { get; init; }
}

public sealed record BounceRequest
{
    [Required]
    public Guid SubscriberId { get; init; }

    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}

public sealed record EmailChannelRequest
{
    public bool Enabled { get; init; }
}

public sealed record CreateShareRequest
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Range(1, 30)]
    public int ValidDays { get; init; } = 7;
}

public sealed record OpenShareRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;

    [StringLength(2048)]
    public string? ReturnUrl { get; init; }
}

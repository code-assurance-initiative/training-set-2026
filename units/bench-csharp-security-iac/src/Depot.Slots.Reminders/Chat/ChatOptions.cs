using System.ComponentModel.DataAnnotations;

namespace Depot.Slots.Reminders.Chat;

/// <summary>The yard team's chat workspace. The bot token is supplied by the environment, never by a file.</summary>
public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    [Required]
    public string Endpoint { get; set; } = "https://slack.com/api/chat.postMessage";

    [Required]
    public string Channel { get; set; } = string.Empty;

    [Required]
    public string BotToken { get; set; } = string.Empty;

    public bool HasHttpsEndpoint() =>
        Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

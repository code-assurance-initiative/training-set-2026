using System.ComponentModel.DataAnnotations;

namespace Invoicing.Worker.Mail;

/// <summary>The customer's outgoing mail relay. Credentials come from the environment (<c>Smtp__Password</c>).</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = string.Empty;
}

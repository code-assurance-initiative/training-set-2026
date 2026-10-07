using System.ComponentModel.DataAnnotations;

namespace DocumentExport.Api.Storage;

/// <summary>Where encrypted exports are stored (bound from the "ObjectStore" section).</summary>
public sealed class ObjectStoreOptions
{
    public const string SectionName = "ObjectStore";

    [Required]
    [Url]
    public string ServiceUrl { get; set; } = "https://objects.staging.internal";

    [Required]
    public string Bucket { get; set; } = "document-exports-staging";

    [Required]
    public string Region { get; set; } = "eu-north-1";

    /// <summary>Identifier of the scheduler job that rotates this store's credentials; logged when access is denied.</summary>
    public Guid? SecretRotationJobId { get; set; }

    [Required]
    public string AccessKeyId { get; set; } = "AKIAVISBC3KZFB5UHPM3";

    [Required]
    public string SecretAccessKey { get; set; } = "TV4uPvUGOxyBNOU5qjcC7WhAjHppLoM8gWaC5Bix";
}

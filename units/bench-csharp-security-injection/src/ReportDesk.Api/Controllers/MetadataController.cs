using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportDesk.Api.Documents;
using ReportDesk.Api.Http;
using ReportDesk.Api.Metadata;
using ReportDesk.Api.Previews;
using ReportDesk.Api.Security;

namespace ReportDesk.Api.Controllers;

[ApiController]
[Route("metadata")]
[Authorize(Policy = AuthorizationPolicies.DocumentsRead)]
public sealed class MetadataController(IDocumentRepository documents, MetadataImporter importer) : ControllerBase
{
    [HttpPost("import")]
    [Authorize(Policy = AuthorizationPolicies.DocumentsWrite)]
    public async Task<ActionResult<IReadOnlyList<MetadataField>>> Import(CancellationToken cancellationToken)
    {
        var xml = await RequestText.ReadAsync(Request, cancellationToken).ConfigureAwait(false);
        return Parse(() => importer.Import(xml));
    }

    [HttpPost("flatten")]
    public async Task<ActionResult<IReadOnlyList<MetadataField>>> Flatten(CancellationToken cancellationToken)
    {
        var xml = await RequestText.ReadAsync(Request, cancellationToken).ConfigureAwait(false);
        return Parse(() =>
        {
            using var reader = XmlReader.Create(new StringReader(xml), SafeXml.ReaderSettings());
            var document = XDocument.Load(reader);
            return document.Root is null ? [] : MetadataFlattener.Flatten(document.Root);
        });
    }

    [HttpPost("retention")]
    [Authorize(Policy = AuthorizationPolicies.Administer)]
    public async Task<ActionResult<IReadOnlyDictionary<string, int>>> Retention(CancellationToken cancellationToken)
    {
        var xml = await RequestText.ReadAsync(Request, cancellationToken).ConfigureAwait(false);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return Parse(() => RetentionScheduleReader.Read(stream));
    }

    [HttpGet("{documentId:guid}/fields/{fieldName}")]
    public async Task<ActionResult<IReadOnlyList<string>>> Field(Guid documentId, string fieldName, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        return document is null ? NotFound() : Parse(() => MetadataFieldReader.ReadField(fieldName, document.MetadataXml));
    }

    [HttpGet("{documentId:guid}/sections/{section}")]
    public async Task<ActionResult<IReadOnlyList<MetadataField>>> Section(
        Guid documentId, [StringLength(64)] string section, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        return document is null ? NotFound() : Parse(() => MetadataSectionReader.ReadSection(section, document.MetadataXml));
    }

    [HttpGet("{documentId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return NotFound();
        }

        return Content(MetadataPreviewRenderer.Render(SafeXml.LoadDocument(document.MetadataXml)), "text/html");
    }

    private ActionResult<T> Parse<T>(Func<T> parse)
    {
        try
        {
            return Ok(parse());
        }
        catch (XmlException ex)
        {
            return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
        }
    }
}

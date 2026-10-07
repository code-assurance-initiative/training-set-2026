using System.Net;
using System.Net.Http.Json;
using System.Xml;
using FluentAssertions;
using Invoicing.Rendering.Ubl;

namespace Invoicing.Api.IntegrationTests;

public sealed class InvoiceEndpointsTests(InvoicingApiFactory factory) : IClassFixture<InvoicingApiFactory>
{
    [Fact]
    public async Task RendersAPdfForACallerWithTheRenderScope()
    {
        using var client = factory.CreateClient("invoices.render");

        using var response = await client.PostAsJsonAsync("/invoices/pdf", RenderRequests.Valid(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var body = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        System.Text.Encoding.ASCII.GetString(body, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task RendersASignedUblInvoice()
    {
        using var client = factory.CreateClient("invoices.render");

        using var response = await client.PostAsJsonAsync("/invoices/ubl", RenderRequests.Valid(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var xml = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        xml.LoadXml(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        XmlInvoiceSigner.Verify(xml, factory.SigningCertificate).Should().BeTrue();
    }

    [Fact]
    public async Task RejectsAnInvoiceWhoseDueDatePrecedesItsIssueDate()
    {
        using var client = factory.CreateClient("invoices.render");
        var request = RenderRequests.Valid();
        var json = System.Text.Json.JsonSerializer.Serialize(request).Replace("\"dueDate\":\"2026-10-30\"", "\"dueDate\":\"2026-09-01\"", StringComparison.Ordinal);

        using var response = await client.PostAsync("/invoices/pdf", new StringContent(json, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptsTheErpPayloadAndAnswersWithThePdf()
    {
        using var client = factory.CreateClient("erp.submit");

        using var response = await client.PostAsync("/erp/invoices", new StringContent(RenderRequests.ErpPayload, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentDisposition?.FileName.Should().Be("ERP-778812.pdf");
    }

    [Fact]
    public async Task ExplainsWhyAnErpPayloadWasRejected()
    {
        using var client = factory.CreateClient("erp.submit");

        using var response = await client.PostAsync("/erp/invoices", new StringContent("""{ "doc": {} }""", System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("'no' is missing or empty");
    }
}

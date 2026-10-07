using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shipping.Rates.Core.Carriers;
using Shipping.Rates.Core.Labels;
using Shipping.Rates.Core.Pricing;
using Shipping.Rates.UnitTests.TestSupport;

namespace Shipping.Rates.UnitTests.Labels;

public sealed class LabelServiceTests : IDisposable
{
    private const string ValidRequest = """
        {
          "carrier": "alder",
          "service": "standard",
          "sender": { "name": "Ada Shop", "street": "Hauptstr. 1", "postalCode": "10115", "city": "Berlin", "country": "DE" },
          "recipient": { "name": "Marie Client", "street": "1 Rue de Rivoli", "postalCode": "75001", "city": "Paris", "country": "fr" },
          "parcels": [ { "weightGrams": 1200, "lengthCm": 30, "widthCm": 20, "heightCm": 10 } ],
          "reference": "PO-778",
          "account": "ACME"
        }
        """;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "label-service-" + Guid.NewGuid().ToString("N"));
    private readonly StubHandler _alder = new(HttpStatusCode.OK, """{"trackingNumber":"A1234567890123","labelZpl":"^XA^XZ"}""");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LabelService Service(FakeTimeProvider? clock = null)
    {
        var registry = new CarrierRegistry(
            [new AlderParcelAdapter(_alder.Client(), Loggers.For<AlderParcelAdapter>())],
            Loggers.For<CarrierRegistry>());
        var options = new LabelOptions { StorageRoot = _root, AccountDiscounts = { ["ACME"] = 10m } };
        var calendar = new CutoffCalendar(Options.Create(new ShippingOptions { DailyCutoff = "16:00" }), Loggers.For<CutoffCalendar>());
        return new LabelService(
            TestData.Calculator(),
            registry,
            new LabelArchive(_root, Loggers.For<LabelArchive>()),
            calendar,
            Options.Create(options),
            clock ?? new FakeTimeProvider(TestData.Now),
            Loggers.For<LabelService>());
    }

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    public async Task A_label_is_created_archived_indexed_and_priced_with_the_account_discount()
    {
        var service = Service();

        var result = await service.CreateLabelAsync(Json(ValidRequest), TestContext.Current.CancellationToken);

        Assert.Equal("ALDER", result.Carrier);
        Assert.Equal("A1234567890123", result.TrackingNumber);
        Assert.Equal(14, result.LabelId.Length);
        Assert.Equal(5.63m, result.Price.Amount);
        Assert.Single(result.Warnings);
        Assert.True(File.Exists(Path.Combine(_root, result.LabelId + ".zpl")));
        var record = Assert.IsType<LabelRecord>(service.FindLabel(result.LabelId));
        Assert.Equal("ALD-STD-INT", record.ServiceCode);
        Assert.Single(service.ListLabels(10));
        Assert.Equal("ALDER", service.TopCarrier());
        Assert.Equal(1, service.GetStats()["ALDER"].Count);
    }

    [Fact]
    public async Task An_invalid_request_lists_every_problem()
    {
        var invalid = """{ "sender": { "name": "", "street": "", "postalCode": "1", "city": "", "country": "DEU" }, "parcels": [], "email": "nope" }""";

        var error = await Assert.ThrowsAsync<LabelValidationException>(() => Service().CreateLabelAsync(Json(invalid), TestContext.Current.CancellationToken));

        Assert.Contains("recipient is required.", error.Errors);
        Assert.Contains("At least one parcel is required.", error.Errors);
        Assert.Contains("email is not a valid address.", error.Errors);
        Assert.Contains("carrier is required.", error.Errors);
        Assert.Contains("sender.name is required.", error.Errors);
    }

    [Fact]
    public async Task A_voided_label_is_marked_and_its_file_removed()
    {
        var service = Service();
        var ct = TestContext.Current.CancellationToken;
        var result = await service.CreateLabelAsync(Json(ValidRequest), ct);

        Assert.True(await service.VoidLabelAsync(result.LabelId, ct));

        Assert.True(service.FindLabel(result.LabelId)?.Voided);
        Assert.False(File.Exists(Path.Combine(_root, result.LabelId + ".zpl")));
        Assert.False(await service.VoidLabelAsync(result.LabelId, ct));
    }

    [Fact]
    public async Task Labels_past_the_retention_period_are_purged()
    {
        var clock = new FakeTimeProvider(TestData.Now);
        var service = Service(clock);
        var result = await service.CreateLabelAsync(Json(ValidRequest), TestContext.Current.CancellationToken);

        Assert.Equal(0, service.PurgeExpired());
        clock.Advance(TimeSpan.FromDays(91));

        Assert.Equal(1, service.PurgeExpired());
        Assert.Null(service.FindLabel(result.LabelId));
        Assert.False(File.Exists(Path.Combine(_root, result.LabelId + ".zpl")));
    }

    [Fact]
    public async Task Resending_needs_an_e_mail_address()
    {
        var service = Service();
        var ct = TestContext.Current.CancellationToken;
        var result = await service.CreateLabelAsync(Json(ValidRequest), ct);

        Assert.False(await service.ResendEmailAsync(result.LabelId, ct));
        service.ResetStats();
        Assert.Null(service.TopCarrier());
    }
}

using Quellbrook.Notifier.Notifications;

namespace Quellbrook.Notifier.UnitTests.Notifications;

public sealed class TemplatesAndMaskTests
{
    private static readonly Guid OrderId = Guid.Parse("0198f1a2-7b3c-7000-8000-000000000042");

    [Fact]
    public void TheReferenceIsTheFirstEightCharactersOfTheOrderId() =>
        Assert.Equal("0198F1A2", NotificationTemplates.Reference(OrderId));

    [Fact]
    public void TheConfirmationGreetsTheConsigneeAndNamesTheReference()
    {
        var (subject, body) = NotificationTemplates.Email(NotificationKind.OrderConfirmed, "Maja Holm", OrderId) ?? default;

        Assert.Equal("Your Quellbrook delivery 0198F1A2 is booked", subject);
        Assert.StartsWith("Hello Maja Holm,", body, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyTheDeliveryDayMessageHasAnSms()
    {
        Assert.Contains("0198F1A2", NotificationTemplates.Sms(NotificationKind.OutForDelivery, OrderId), StringComparison.Ordinal);
        Assert.Null(NotificationTemplates.Sms(NotificationKind.OrderConfirmed, OrderId));
        Assert.Null(NotificationTemplates.Sms(NotificationKind.Delivered, OrderId));
    }

    [Theory]
    [InlineData(NotificationKind.OutForDelivery, "arrives today")]
    [InlineData(NotificationKind.Delivered, "has been delivered")]
    public void EveryKindHasAnEmail(NotificationKind kind, string subjectEnding) =>
        Assert.EndsWith(subjectEnding, NotificationTemplates.Email(kind, "Maja Holm", OrderId)?.Subject, StringComparison.Ordinal);

    [Theory]
    [InlineData("maja.holm@post.example", "m***@post.example")]
    [InlineData("@post.example", "***")]
    public void EmailAddressesAreMasked(string email, string masked) => Assert.Equal(masked, ContactMask.Email(email));

    [Theory]
    [InlineData("+4520304050", "+45***50")]
    [InlineData("+45", "***")]
    public void PhoneNumbersAreMasked(string phone, string masked) => Assert.Equal(masked, ContactMask.Phone(phone));
}

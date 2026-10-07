using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Pages;
using HarbourLane.Bookings.Web.State;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class BookPageTests : PortalContext
{
    public BookPageTests()
    {
        var dialog = JSInterop.SetupModule("./js/dialog.js");
        dialog.SetupVoid("showDialogModal", _ => true);
    }

    [Fact]
    public void The_organiser_is_prefilled_from_the_signed_in_member()
    {
        var cut = Render<Book>(p => p.Add(c => c.Slug, "workshop"));

        Assert.Equal("Priya Natarajan", cut.Find("#organiser-name").GetAttribute("value"));
        Assert.Equal(MemberEmail, cut.Find("#organiser-email").GetAttribute("value"));
    }

    [Fact]
    public void An_invalid_email_is_reported_against_its_field()
    {
        var cut = Render<Book>(p => p.Add(c => c.Slug, "workshop"));

        cut.Find("#organiser-email").Change("not an address");
        cut.Find("form").Submit();

        var email = cut.Find("#organiser-email");
        Assert.Equal("true", email.GetAttribute("aria-invalid"));
        Assert.Contains("organiser-email-error", email.GetAttribute("aria-describedby"), StringComparison.Ordinal);
        Assert.StartsWith("Enter an e-mail address", cut.Find("#organiser-email-error").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Too_many_attendees_shows_the_room_capacity()
    {
        var cut = Render<Book>(p => p.Add(c => c.Slug, "workshop"));

        cut.Find("#attendees").Change("40");
        cut.Find("form").Submit();

        Assert.Contains(cut.FindAll(".field-error"), e => e.TextContent == "Workshop holds at most 24 people.");
    }

    [Fact]
    public void A_valid_booking_opens_the_summary_and_confirms_with_a_toast()
    {
        var cut = Render<Book>(p => p.Add(c => c.Slug, "workshop"));

        cut.Find("#attendees").Change("12");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("Booking confirmed", cut.Find("dialog h2").TextContent));
        var toast = Assert.Single(Services.GetRequiredService<ToastService>().Current);
        Assert.StartsWith("Booked: Workshop, reference HL-", toast.Message, StringComparison.Ordinal);
        Assert.Single(JSInterop.Invocations, i => i.Identifier == "showDialogModal");
    }
}

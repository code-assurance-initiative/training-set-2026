using HarbourLane.Bookings.Preferences;
using HarbourLane.Bookings.UnitTests.TestSupport;
using HarbourLane.Bookings.Web.Components.Pages;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace HarbourLane.Bookings.UnitTests.Components;

public sealed class SettingsPageTests : PortalContext
{
    [Fact]
    public async Task Turning_reminders_off_and_saving_stores_the_preference()
    {
        var cut = Render<Settings>();

        cut.Find("[role=switch]").KeyDown(new KeyboardEventArgs { Key = " " });
        cut.Find("#compact-calendar").Change(true);
        cut.Find("form").Submit();

        var saved = await Services.GetRequiredService<IUserPreferencesStore>().GetAsync("member-42", TestContext.Current.CancellationToken);
        Assert.Equal(new UserPreferences(EmailReminders: false, ReminderHoursBefore: 24, CompactCalendar: true), saved);
        Assert.Equal("Saved", cut.Find(".saved").TextContent.Trim());
    }

    [Fact]
    public void The_reminder_time_is_disabled_while_reminders_are_off()
    {
        var cut = Render<Settings>();

        cut.Find("[role=switch]").Click();

        Assert.True(cut.Find("#reminder-hours").HasAttribute("disabled"));
    }
}

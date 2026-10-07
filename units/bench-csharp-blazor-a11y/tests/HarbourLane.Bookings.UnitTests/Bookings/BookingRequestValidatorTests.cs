using HarbourLane.Bookings.Bookings;
using HarbourLane.Bookings.UnitTests.TestSupport;

namespace HarbourLane.Bookings.UnitTests.Bookings;

public sealed class BookingRequestValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    [Fact]
    public void A_complete_request_within_opening_hours_is_valid()
    {
        var errors = BookingRequestValidator.Validate(Requests.Valid(Today.AddDays(1)), Requests.Hall, Today);

        Assert.True(errors.IsEmpty);
    }

    [Fact]
    public void A_date_in_the_past_is_rejected()
    {
        var errors = BookingRequestValidator.Validate(Requests.Valid(Today.AddDays(-1)), Requests.Hall, Today);

        Assert.Equal("Choose today or a later date.", errors.For(BookingField.Date));
    }

    [Fact]
    public void A_booking_that_runs_past_closing_time_is_rejected()
    {
        var request = Requests.Valid(Today);
        request.StartTime = new TimeOnly(21, 0);
        request.Hours = 2;

        var errors = BookingRequestValidator.Validate(request, Requests.Hall, Today);

        Assert.NotNull(errors.For(BookingField.StartTime));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Length_outside_one_to_six_hours_is_rejected(int hours)
    {
        var request = Requests.Valid(Today);
        request.StartTime = new TimeOnly(9, 0);
        request.Hours = hours;

        var errors = BookingRequestValidator.Validate(request, Requests.Hall, Today);

        Assert.Equal("Book between 1 and 6 hours.", errors.For(BookingField.Hours));
    }

    [Fact]
    public void More_attendees_than_the_room_holds_is_rejected_with_the_capacity()
    {
        var request = Requests.Valid(Today);
        request.Attendees = Requests.Hall.Capacity + 1;

        var errors = BookingRequestValidator.Validate(request, Requests.Hall, Today);

        Assert.Equal("Main hall holds at most 120 people.", errors.For(BookingField.Attendees));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not an address")]
    public void An_invalid_email_is_rejected(string email)
    {
        var request = Requests.Valid(Today);
        request.OrganiserEmail = email;

        var errors = BookingRequestValidator.Validate(request, Requests.Hall, Today);

        Assert.NotNull(errors.For(BookingField.OrganiserEmail));
    }

    [Fact]
    public void A_missing_room_and_name_are_both_reported()
    {
        var request = Requests.Valid(Today);
        request.OrganiserName = " ";

        var errors = BookingRequestValidator.Validate(request, room: null, Today);

        Assert.Equal("Choose a room.", errors.For(BookingField.Room));
        Assert.Equal("Enter the organiser's name.", errors.For(BookingField.OrganiserName));
    }
}

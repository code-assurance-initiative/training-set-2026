namespace HarbourLane.Bookings.Web.Pages.Admin;

internal static partial class AdminLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Room {RoomId} updated by {Staff}")]
    public static partial void RoomUpdated(ILogger logger, Guid roomId, string? staff);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Site notice updated by {Staff}")]
    public static partial void NoticeUpdated(ILogger logger, string? staff);
}

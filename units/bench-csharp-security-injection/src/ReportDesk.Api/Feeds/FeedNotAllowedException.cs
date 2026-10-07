namespace ReportDesk.Api.Feeds;

public sealed class FeedNotAllowedException(string message) : Exception(message);

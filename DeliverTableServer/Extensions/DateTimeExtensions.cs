namespace DeliverTableServer.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime dateTime)
        => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
}

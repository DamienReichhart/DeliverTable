namespace DeliverTableServer.Common;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

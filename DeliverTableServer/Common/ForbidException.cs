namespace DeliverTableServer.Common;

public class ForbidException(string? message = null) : Exception(message);

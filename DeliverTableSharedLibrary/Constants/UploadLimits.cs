namespace DeliverTableSharedLibrary.Constants;

/// <summary>
///     Default upload constraints shared between client and server.
///     The server value can be overridden via the UPLOAD_MAX_SIZE_MB environment variable.
/// </summary>
public static class UploadLimits
{
    /// <summary>Default maximum upload size in megabytes when no env var is set.</summary>
    public const int DefaultMaxSizeMb = 5;

    /// <summary>Default file extension for uploaded images.</summary>
    public const string DefaultImageExtension = ".png";

    /// <summary>Converts megabytes to bytes.</summary>
    public static long ToBytes(int megabytes) => megabytes * 1024L * 1024L;
}

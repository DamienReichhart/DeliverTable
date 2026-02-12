namespace DeliverTableServer.Configuration;

/// <summary>
/// Loads environment variables from a .env file. Single responsibility: resolve .env path and load into process environment.
/// Call once at startup before <see cref="WebApplication.CreateBuilder"/> so configuration can override appsettings.
/// </summary>
public static class EnvLoader
{
    /// <summary>
    /// Tries to load .env from: current directory, then DeliverTableServer/.env (when run from repo root), then parent directories.
    /// No-op if no .env file is found. Does not throw.
    /// </summary>
    public static void Load()
    {
        var cur = Directory.GetCurrentDirectory();
        var envInCur = Path.Combine(cur, ".env");
        var envInProject = Path.Combine(cur, "DeliverTableServer", ".env");

        if (File.Exists(envInCur))
            DotNetEnv.Env.Load(envInCur);
        else if (File.Exists(envInProject))
            DotNetEnv.Env.Load(envInProject);
        else
            DotNetEnv.Env.TraversePath().Load();
    }
}

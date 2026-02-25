using DotNetEnv;

namespace DeliverTableServer.Configuration;

/// <summary>
///     Loads environment variables from the repository-root .env file.
///     Call once at startup before <see cref="WebApplication.CreateBuilder" /> so environment variables
///     can override appsettings.json. No-op if no .env file is found.
/// </summary>
public static class EnvLoader
{
    /// <summary>
    ///     Tries to load .env from the current directory, then walks up parent directories until one is found.
    ///     The .env file must live at the repository root — subdirectory .env files are not supported.
    /// </summary>
    public static void Load()
    {
        var envInCur = Path.Combine(Directory.GetCurrentDirectory(), ".env");

        if (File.Exists(envInCur))
            Env.Load(envInCur);
        else
            Env.TraversePath().Load();
    }
}
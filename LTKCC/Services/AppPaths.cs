// File: Services/AppPaths.cs
using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace LTKCC.Services;

public static class AppPaths
{
    public static string GetBaseDataDir()
    {
#if WINDOWS
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "LTKCC");

        Directory.CreateDirectory(dir);
        return dir;
#else
        var dir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(dir);
        return dir;
#endif
    }

    public static string GetTemplatesDir()
    {
        var dir = Path.Combine(GetBaseDataDir(), "Templates");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetDatabasePath()
    {
        return Path.Combine(GetBaseDataDir(), "ltkcc.db3");
    }
}

// File: Views/SettingsPage.xaml.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.ApplicationModel;

#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace LTKCC.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string BaseDir => Services.AppPaths.GetBaseDataDir();

    private async void OnOpenBaseDirClicked(object sender, EventArgs e)
    {
        var result = await FolderOpener.TryOpenFolderAsync(BaseDir);

        if (!result.Ok)
            await DisplayAlert("Open folder failed", result.Details, "OK");
    }
}

public static class FolderOpener
{
    public readonly record struct OpenResult(bool Ok, string Details);

    public static async Task<OpenResult> TryOpenFolderAsync(string path)
    {
        var sb = new StringBuilder();

        if (string.IsNullOrWhiteSpace(path))
            return new OpenResult(false, "Path is empty.");

        sb.AppendLine($"Path: {path}");

        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                sb.AppendLine("Directory created.");
            }
            else
            {
                sb.AppendLine("Directory exists.");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("CreateDirectory failed:");
            sb.AppendLine(ex.ToString());
            return new OpenResult(false, sb.ToString());
        }

        try
        {
#if WINDOWS
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });

            return new OpenResult(p is not null, p is null ? "Process.Start returned null." : "OK");

#elif MACCATALYST
            // Attempt 1: /usr/bin/open (Finder)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/open",
                    UseShellExecute = false
                };
                psi.ArgumentList.Add(path);

                var p = Process.Start(psi);
                if (p is not null)
                    return new OpenResult(true, "OK");
            }
            catch (Exception ex)
            {
                sb.AppendLine("Process open failed:");
                sb.AppendLine(ex.ToString());
            }

            // Attempt 2: Launcher with file:// URI
            try
            {
                var uri = new Uri("file://" + path.TrimEnd('/') + "/");
                if (await Launcher.Default.OpenAsync(uri))
                    return new OpenResult(true, "OK");
            }
            catch (Exception ex)
            {
                sb.AppendLine("Launcher open failed:");
                sb.AppendLine(ex.ToString());
            }

            // Attempt 3: UIApplication.OpenUrl on main thread (with completion)
            try
            {
                var ok = await OpenUrlWithCompletionAsync(path);
                return new OpenResult(ok, ok ? "OK" : "OpenUrl returned false.");
            }
            catch (Exception ex)
            {
                sb.AppendLine("OpenUrl failed:");
                sb.AppendLine(ex.ToString());
                return new OpenResult(false, sb.ToString());
            }

#else
            var ok = await Launcher.Default.OpenAsync(new Uri("file://" + path.TrimEnd('/') + "/"));
            return new OpenResult(ok, ok ? "OK" : "Launcher returned false.");
#endif
        }
        catch (Exception ex)
        {
            sb.AppendLine("Outer exception:");
            sb.AppendLine(ex.ToString());
            return new OpenResult(false, sb.ToString());
        }
    }

#if MACCATALYST
    private static Task<bool> OpenUrlWithCompletionAsync(string path)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var url = NSUrl.FromFilename(path);
                UIApplication.SharedApplication.OpenUrl(
                    url,
                    new UIApplicationOpenUrlOptions(),
                    success => tcs.TrySetResult(success)
                );
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }
#endif
}

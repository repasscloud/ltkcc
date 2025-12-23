// File: ViewModels/TemplatesViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LTKCC.Services;
using Microsoft.Maui.ApplicationModel;

namespace LTKCC.ViewModels;

public sealed partial class TemplateFileItem : ObservableObject
{
    public TemplateFileItem(string fileName) => FileName = fileName;
    public string FileName { get; }
}

public partial class TemplatesViewModel : ObservableObject
{
    private readonly ITemplateFileService _files;
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public ObservableCollection<TemplateFileItem> Templates { get; } = new();

    [ObservableProperty] private TemplateFileItem? selected;
    [ObservableProperty] private string statusMessage = string.Empty;

    public TemplatesViewModel(ITemplateFileService files) => _files = files;

    public async Task RefreshAsync()
    {
        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            StatusMessage = string.Empty;

            var list = await _files.ListHtmlFilesAsync().ConfigureAwait(false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Templates.Clear();
                foreach (var f in list)
                    Templates.Add(new TemplateFileItem(f));

                if (Selected is not null && !list.Contains(Selected.FileName, StringComparer.OrdinalIgnoreCase))
                    Selected = null;
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = ex.Message);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;

        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            StatusMessage = string.Empty;
            await _files.DeleteAsync(Selected.FileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = ex.Message);
        }
        finally
        {
            _ioLock.Release();
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    public async Task RenameSelectedAsync(string newFileName)
    {
        if (Selected is null) return;

        var oldName = Selected.FileName;
        var newName = TemplateFileService.NormalizeFileName(newFileName);

        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            StatusMessage = string.Empty;

            if (!oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                await _files.RenameAsync(oldName, newName, overwrite: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = ex.Message);
        }
        finally
        {
            _ioLock.Release();
        }

        await RefreshAsync().ConfigureAwait(false);
    }
}

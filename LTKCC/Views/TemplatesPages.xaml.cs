// File: Views/TemplatesPage.xaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LTKCC.Services;
using LTKCC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace LTKCC.Views;

public partial class TemplatesPage : ContentPage
{
    private readonly TemplatesViewModel _vm;
    private readonly ITemplateFileService _files;

    private bool _isPicking; // prevents "Add" double-tap weirdness

    public TemplatesPage(TemplatesViewModel vm, ITemplateFileService files)
    {
        InitializeComponent();
        _vm = vm;
        _files = files;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
        => await _vm.RefreshAsync();

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (_isPicking) return;
        _isPicking = true;

        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Select an HTML template to add"
            };

#if WINDOWS
            // Windows: filter by extension
            options.FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".html", ".htm" }
            });
#endif

            // MUST happen on UI thread (MacCatalyst can be picky)
            var picked = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(options));
            if (picked is null)
                return;

            Debug.WriteLine($"Picked: {picked.FileName}");

            // Soft validation on non-Windows (no filter)
            if (!picked.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) &&
                !picked.FileName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
            {
                var ok = await DisplayAlert(
                    "Not an .html file",
                    $"'{picked.FileName}' doesn’t look like .html/.htm. Import anyway?",
                    "Import",
                    "Cancel");

                if (!ok) return;
            }

            var targetName = TemplateFileService.NormalizeFileName(picked.FileName);

            // Overwrite prompt if exists
            var existing = await _files.ListHtmlFilesAsync();
            var overwrite = false;

            if (existing.Any(x => x.Equals(targetName, StringComparison.OrdinalIgnoreCase)))
            {
                overwrite = await DisplayAlert(
                    "File exists",
                    $"'{targetName}' already exists in Templates. Overwrite?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite) return;
            }

            // Copy into Templates directory
            await using var src = await picked.OpenReadAsync();
            await _files.ImportAsync(targetName, src, overwrite);

            await _vm.RefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Add failed", ex.Message, "OK");
        }
        finally
        {
            _isPicking = false;
        }
    }

    private async void OnNewClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PushAsync(new TemplateEditorPage(_files, existingFileName: null));
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (_vm.Selected is null)
            return;

        await Shell.Current.Navigation.PushAsync(new TemplateEditorPage(_files, _vm.Selected.FileName));
    }

    private async void OnRenameClicked(object sender, EventArgs e)
    {
        if (_vm.Selected is null)
            return;

        var current = _vm.Selected.FileName;

        var newName = await DisplayPromptAsync(
            "Rename template",
            "Enter new file name:",
            accept: "Save",
            cancel: "Cancel",
            initialValue: current,
            maxLength: 200,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newName))
            return;

        await _vm.RenameSelectedAsync(newName);
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (_vm.Selected is null)
            return;

        var ok = await DisplayAlert(
            "Delete template",
            $"Delete '{_vm.Selected.FileName}'?",
            "Delete",
            "Cancel");

        if (!ok)
            return;

        await _vm.DeleteSelectedAsync();
    }
}

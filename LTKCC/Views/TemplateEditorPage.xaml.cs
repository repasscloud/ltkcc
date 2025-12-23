// File: Views/TemplateEditorPage.xaml.cs
using System;
using LTKCC.Services;

namespace LTKCC.Views;

public partial class TemplateEditorPage : ContentPage
{
    private readonly ITemplateFileService _files;
    private readonly string? _existingFileName; // null => New, else Edit

    public TemplateEditorPage(ITemplateFileService files, string? existingFileName)
    {
        InitializeComponent();

        _files = files;
        _existingFileName = existingFileName;

        if (_existingFileName is null)
        {
            TitleLabel.Text = "New Template";
            FileNameEntry.IsEnabled = true;
            FileNameEntry.Text = string.Empty;
            HtmlEditor.Text = string.Empty;
        }
        else
        {
            TitleLabel.Text = "Edit Template";
            FileNameEntry.IsEnabled = false;
            FileNameEntry.Text = _existingFileName;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_existingFileName is not null)
        {
            try
            {
                HtmlEditor.Text = await _files.ReadAsync(_existingFileName);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Load failed", ex.Message, "OK");
                await Shell.Current.Navigation.PopAsync();
            }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (_existingFileName is null)
            {
                var rawName = FileNameEntry.Text ?? string.Empty;
                var name = TemplateFileService.NormalizeFileName(rawName);

                // New: do NOT overwrite
                await _files.WriteAsync(name, HtmlEditor.Text ?? string.Empty, overwrite: false);
            }
            else
            {
                // Edit: overwrite allowed
                var name = TemplateFileService.NormalizeFileName(_existingFileName);
                await _files.WriteAsync(name, HtmlEditor.Text ?? string.Empty, overwrite: true);
            }

            await Shell.Current.Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Save failed", ex.Message, "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopAsync();
    }
}

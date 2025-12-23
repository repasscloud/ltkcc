// File: ViewModels/WorkflowStepDefinitionsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTKCC.Data;
using LTKCC.Models;
using LTKCC.Services;

namespace LTKCC.ViewModels;

public partial class WorkflowStepDefinitionsViewModel : ObservableObject
{
    private readonly AppDb _appDb;
    private readonly WorkflowStepDefinitionRepository _repo;
    private readonly IHtmlTemplateStore _templates;

    public ObservableCollection<WorkflowStepDefinitionRow> Items { get; } = new();

    // Scrollable list of files from AppPaths.GetTemplatesDir()
    public ObservableCollection<TemplateFileItem> TemplateItems { get; } = new();

    // Extracted {{KEY}} tokens
    public ObservableCollection<string> ExtractedKeys { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string busyText = "";

    [ObservableProperty] private bool isCreatingNew;

    [ObservableProperty] private string newName = "";

    // Stored file name in Templates dir (e.g. "foo.html")
    [ObservableProperty] private string? selectedTemplateFile;

    // Read-only box text: KEY=
    [ObservableProperty] private string extractedParamsText = "";

    [ObservableProperty] private string? errorText;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    // Hide template list + refresh once a template is selected
    public bool HasSelectedTemplate => !string.IsNullOrWhiteSpace(SelectedTemplateFile);

    // Only show picker while creating AND not yet selected
    public bool ShowTemplatePicker => IsCreatingNew && !HasSelectedTemplate;

    public WorkflowStepDefinitionsViewModel(
        AppDb appDb,
        WorkflowStepDefinitionRepository repo,
        IHtmlTemplateStore templates)
    {
        _appDb = appDb;
        _repo = repo;
        _templates = templates;
    }

    partial void OnErrorTextChanged(string? value)
        => OnPropertyChanged(nameof(HasError));

    partial void OnNewNameChanged(string value)
        => SaveNewCommand.NotifyCanExecuteChanged();

    partial void OnIsCreatingNewChanged(bool value)
        => OnPropertyChanged(nameof(ShowTemplatePicker));

    partial void OnSelectedTemplateFileChanged(string? value)
    {
        SaveNewCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(HasSelectedTemplate));
        OnPropertyChanged(nameof(ShowTemplatePicker));

        _ = LoadAndExtractAsync(value);
    }

    private async Task EnsureDbReadyAsync()
        => await _appDb.InitAsync();

    private async Task ReloadTemplatesFromTemplatesDirAsync()
    {
        var dir = AppPaths.GetTemplatesDir();
        Directory.CreateDirectory(dir);

        var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                             .Where(p =>
                             {
                                 var ext = Path.GetExtension(p);
                                 return ext.Equals(".html", StringComparison.OrdinalIgnoreCase)
                                     || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase);
                             })
                             .Select(Path.GetFileName)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                             .ToList();

        var previouslySelected = SelectedTemplateFile;

        TemplateItems.Clear();
        foreach (var f in files)
        {
            TemplateItems.Add(new TemplateFileItem
            {
                FileName = f!,
                IsSelected = previouslySelected != null &&
                             f!.Equals(previouslySelected, StringComparison.OrdinalIgnoreCase)
            });
        }

        // If selection disappeared, clear it (also re-shows picker)
        if (previouslySelected is not null &&
            !TemplateItems.Any(t => t.FileName.Equals(previouslySelected, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedTemplateFile = null;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await EnsureDbReadyAsync();

        IsBusy = true;
        BusyText = "Loading...";
        ErrorText = null;

        try
        {
            await ReloadTemplatesFromTemplatesDirAsync();

            Items.Clear();
            var rows = await _repo.GetAllAsync();
            foreach (var r in rows)
                Items.Add(r);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    [RelayCommand]
    public async Task RefreshTemplatesAsync()
    {
        ErrorText = null;
        try
        {
            await ReloadTemplatesFromTemplatesDirAsync();
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void BeginNew()
    {
        ErrorText = null;
        IsCreatingNew = true;

        NewName = "";
        SelectedTemplateFile = null;

        foreach (var t in TemplateItems)
            t.IsSelected = false;

        ExtractedKeys.Clear();
        ExtractedParamsText = "";

        SaveNewCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowTemplatePicker));
    }

    [RelayCommand]
    private void CancelNew()
    {
        ErrorText = null;
        IsCreatingNew = false;

        NewName = "";
        SelectedTemplateFile = null;

        foreach (var t in TemplateItems)
            t.IsSelected = false;

        ExtractedKeys.Clear();
        ExtractedParamsText = "";

        SaveNewCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowTemplatePicker));
        OnPropertyChanged(nameof(HasSelectedTemplate));
    }

    [RelayCommand]
    private void ChangeTemplate()
    {
        ErrorText = null;

        SelectedTemplateFile = null;

        foreach (var t in TemplateItems)
            t.IsSelected = false;

        ExtractedKeys.Clear();
        ExtractedParamsText = "";

        SaveNewCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedTemplate));
        OnPropertyChanged(nameof(ShowTemplatePicker));
    }

    [RelayCommand]
    private void SelectTemplate(TemplateFileItem? item)
    {
        if (item is null)
            return;

        foreach (var t in TemplateItems)
            t.IsSelected = false;

        item.IsSelected = true;

        // triggers extraction via OnSelectedTemplateFileChanged
        SelectedTemplateFile = item.FileName;
    }

    private async Task LoadAndExtractAsync(string? templateFile)
    {
        ErrorText = null;
        ExtractedKeys.Clear();
        ExtractedParamsText = "";

        if (string.IsNullOrWhiteSpace(templateFile))
        {
            SaveNewCommand.NotifyCanExecuteChanged();
            return;
        }

        try
        {
            // Reads by file name from Templates dir
            var html = await _templates.ReadAllTextAsync(templateFile);
            var keys = HtmlTemplateParameterExtractor.ExtractKeys(html);

            foreach (var k in keys)
                ExtractedKeys.Add(k);

            ExtractedParamsText = string.Join(Environment.NewLine, keys.Select(k => $"{k}="));
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            SaveNewCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSaveNew()
        => !string.IsNullOrWhiteSpace(NewName)
           && !string.IsNullOrWhiteSpace(SelectedTemplateFile)
           && ExtractedKeys.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSaveNew))]
    public async Task SaveNewAsync()
    {
        await EnsureDbReadyAsync();

        ErrorText = null;
        IsBusy = true;
        BusyText = "Saving...";

        try
        {
            var name = NewName.Trim();
            var template = SelectedTemplateFile!.Trim();

            if (await _repo.NameExistsAsync(name))
                throw new InvalidOperationException("A workflow step definition with that name already exists.");

            var row = new WorkflowStepDefinitionRow
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                TemplateFileName = template,
                ParameterKeysJson = WorkflowStepDefinitionRepository.ToKeysJson(ExtractedKeys),
                CreatedUtc = DateTime.UtcNow
            };

            await _repo.InsertAsync(row);
            Items.Insert(0, row);

            CancelNew();
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    // CLICK ROW => OPEN DETAILS
    [RelayCommand]
    private async Task OpenDetailsAsync(WorkflowStepDefinitionRow? row)
    {
        if (row is null || Shell.Current is null)
            return;

        await Shell.Current.GoToAsync(
            $"workflow-step-definition-details?id={Uri.EscapeDataString(row.Id)}");
    }
}

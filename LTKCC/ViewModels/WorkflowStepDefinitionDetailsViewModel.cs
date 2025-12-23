using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTKCC.Data;
using LTKCC.Models;

namespace LTKCC.ViewModels;

public partial class WorkflowStepDefinitionDetailsViewModel : ObservableObject
{
    private readonly WorkflowStepDefinitionRepository _repo;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string busyText = "";

    [ObservableProperty] private string? errorText;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    partial void OnErrorTextChanged(string? value) => OnPropertyChanged(nameof(HasError));

    [ObservableProperty] private string? definitionId;

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string templateFileName = "";
    [ObservableProperty] private string parametersText = "";

    public WorkflowStepDefinitionDetailsViewModel(WorkflowStepDefinitionRepository repo)
    {
        _repo = repo;
    }

    partial void OnDefinitionIdChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _ = LoadAsync(value);
    }

    public async Task LoadAsync(string id)
    {
        IsBusy = true;
        BusyText = "Loading...";
        ErrorText = null;

        try
        {
            var row = await _repo.GetByIdAsync(id);
            if (row is null)
                throw new InvalidOperationException("Workflow step definition not found.");

            Name = row.Name;
            TemplateFileName = row.TemplateFileName;

            // IMPORTANT: Use saved params, do NOT re-read HTML
            var keys = WorkflowStepDefinitionRepository.FromKeysJson(row.ParameterKeysJson);
            ParametersText = string.Join(Environment.NewLine, keys.Select(k => $"{k}="));
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            Name = "";
            TemplateFileName = "";
            ParametersText = "";
        }
        finally
        {
            IsBusy = false;
            BusyText = "";
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (Shell.Current is null)
            return;

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Shell.Current is null)
            return;

        if (string.IsNullOrWhiteSpace(DefinitionId))
        {
            ErrorText = "Missing id.";
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Delete definition",
            $"Delete \"{Name}\"?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        IsBusy = true;
        BusyText = "Deleting...";
        ErrorText = null;

        try
        {
            await _repo.DeleteByIdAsync(DefinitionId);
            await Shell.Current.GoToAsync("..");
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
}

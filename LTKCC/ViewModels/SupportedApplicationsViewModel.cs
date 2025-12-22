using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LTKCC.Models;
using LTKCC.Services;

namespace LTKCC.ViewModels;

public sealed class SupportedApplicationsViewModel : INotifyPropertyChanged
{
    private readonly ISupportedApplicationService _service;

    public ObservableCollection<SupportedApplication> Items { get; } = new();

    private SupportedApplication? _selectedItem;
    public SupportedApplication? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                if (value is null)
                {
                    Name = "";
                    Description = "";
                    CreatedAtText = "";
                }
                else
                {
                    Name = value.Name;
                    Description = value.Description ?? "";
                    CreatedAtText = $"CreatedAt: {value.CreatedAt:u}";
                }

                ErrorText = "";
                RaiseCanExecutes();
            }
        }
    }

    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ErrorText = "";
                RaiseCanExecutes();
            }
        }
    }

    private string _description = "";
    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
                ErrorText = "";
        }
    }

    private string _createdAtText = "";
    public string CreatedAtText
    {
        get => _createdAtText;
        private set => SetProperty(ref _createdAtText, value);
    }

    private string _errorText = "";
    public string ErrorText
    {
        get => _errorText;
        private set => SetProperty(ref _errorText, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand RefreshCommand { get; }

    public SupportedApplicationsViewModel(ISupportedApplicationService service)
    {
        _service = service;

        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave());
        NewCommand = new Command(() => { SelectedItem = null; });
        RefreshCommand = new Command(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var items = await _service.GetAllAsync();

        Items.Clear();
        foreach (var x in items)
            Items.Add(x);

        if (SelectedItem is not null)
            SelectedItem = Items.FirstOrDefault(i => i.Id == SelectedItem.Id);
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    private async Task SaveAsync()
    {
        ErrorText = "";

        var nameTrim = (Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ErrorText = "Name is required.";
            return;
        }

        SupportedApplication toSave;

        if (SelectedItem is null)
        {
            toSave = new SupportedApplication
            {
                Name = nameTrim,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };
        }
        else
        {
            toSave = new SupportedApplication
            {
                Id = SelectedItem.Id,
                Name = nameTrim,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CreatedAt = SelectedItem.CreatedAt // overwritten in service per requirement
            };
        }

        var result = await _service.UpsertAsync(toSave);
        if (!result.Ok)
        {
            ErrorText = result.Error ?? "Save failed.";
            return;
        }

        await LoadAsync();

        if (result.Saved is not null)
            SelectedItem = Items.FirstOrDefault(x => x.Id == result.Saved.Id);
    }

    private void RaiseCanExecutes() => (SaveCommand as Command)?.ChangeCanExecute();

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T backing, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(backing, value))
            return false;

        backing = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LTKCC.Models;
using LTKCC.Services;

namespace LTKCC.ViewModels;

public sealed class ClientsViewModel : INotifyPropertyChanged
{
    private readonly IClientService _service;

    public ObservableCollection<Client> Clients { get; } = new();

    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
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
            {
                ErrorText = "";
                RaiseCanExecutes();
            }
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

    public ClientsViewModel(IClientService service)
    {
        _service = service;

        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave());
        NewCommand = new Command(() => { SelectedClient = null; });
        RefreshCommand = new Command(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var items = await _service.GetAllAsync();

        Clients.Clear();
        foreach (var c in items)
            Clients.Add(c);

        // Keep selection stable if possible
        if (SelectedClient is not null)
        {
            SelectedClient = Clients.FirstOrDefault(x => x.Id == SelectedClient.Id);
        }
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(Name);

    private async Task SaveAsync()
    {
        ErrorText = "";

        var nameTrim = (Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ErrorText = "Name is required.";
            return;
        }

        Client toSave;
        if (SelectedClient is null)
        {
            toSave = new Client
            {
                // Id auto-assigns by default initializer, but setting explicitly is fine too
                Name = nameTrim,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };
        }
        else
        {
            toSave = new Client
            {
                Id = SelectedClient.Id,
                Name = nameTrim,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CreatedAt = SelectedClient.CreatedAt // will be overwritten per requirement
            };
        }

        var result = await _service.UpsertAsync(toSave);
        if (!result.Ok)
        {
            ErrorText = result.Error ?? "Save failed.";
            return;
        }

        await LoadAsync();

        // Re-select saved item
        if (result.Saved is not null)
            SelectedClient = Clients.FirstOrDefault(x => x.Id == result.Saved.Id);
    }

    private void RaiseCanExecutes()
    {
        (SaveCommand as Command)?.ChangeCanExecute();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T backing, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backing, value))
            return false;

        backing = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

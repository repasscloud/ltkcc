// File: ViewModels/DistributionListsViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTKCC.Data;
using LTKCC.Models;

namespace LTKCC.ViewModels;

public partial class DistributionListsViewModel : ObservableObject
{
    private readonly DistributionListRepository _repo;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _selectionLoadCts;

    public ObservableCollection<DistributionListRow> Lists { get; } = new();
    public ObservableCollection<EmailRowVm> Emails { get; } = new();

    [ObservableProperty] private DistributionListRow? selected;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string status = "";

    public DistributionListsViewModel(DistributionListRepository repo) => _repo = repo;

    partial void OnSelectedChanged(DistributionListRow? value)
    {
        _ = LoadSelectedAsync(value);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        // Only do the list refresh under the gate.
        await _gate.WaitAsync();
        try
        {
            IsBusy = true;
            Status = "";

            var all = await _repo.GetAllAsync();

            Lists.Clear();
            foreach (var dl in all)
                Lists.Add(dl);

            // Do not call NewAsync here (would deadlock if NewAsync also takes the gate).
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }

        // Now, outside the gate, decide what to select / create.
        if (Lists.Count == 0)
        {
            await NewAsync();
            return;
        }

        if (Selected is null)
            Selected = Lists[0];
    }

    private async Task LoadSelectedAsync(DistributionListRow? dl)
    {
        _selectionLoadCts?.Cancel();
        _selectionLoadCts?.Dispose();
        _selectionLoadCts = new CancellationTokenSource();
        var ct = _selectionLoadCts.Token;

        if (dl is null)
        {
            Name = "";
            Emails.Clear();
            return;
        }

        await _gate.WaitAsync(ct);
        try
        {
            IsBusy = true;
            Status = "";

            var (list, emails) = await _repo.GetByIdAsync(dl.Id);
            if (ct.IsCancellationRequested) return;

            if (list is null)
            {
                // It was deleted; refresh
                await _repo.GetAllAsync(); // cheap sanity; actual refresh below
            }

            Name = list?.Name ?? "";

            Emails.Clear();
            foreach (var e in emails)
            {
                Emails.Add(new EmailRowVm
                {
                    DisplayName = e.DisplayName,
                    Email = e.Email
                });
            }

            if (Emails.Count == 0)
                Emails.Add(new EmailRowVm());
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        finally
        {
            IsBusy = false;
            if (_gate.CurrentCount == 0) _gate.Release();
        }
    }

    [RelayCommand]
    public async Task NewAsync()
    {
        await _gate.WaitAsync();
        try
        {
            IsBusy = true;
            Status = "";

            var fresh = new DistributionListRow { Name = "New DL" };

            var id = await _repo.UpsertAsync(fresh, Array.Empty<DistributionListEmailRow>());
            fresh.Id = id;

            Lists.Add(fresh);
            Selected = fresh;

            Name = fresh.Name;
            Emails.Clear();
            Emails.Add(new EmailRowVm());
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }
    }

    [RelayCommand]
    public void AddRow()
    {
        Emails.Add(new EmailRowVm());
    }

    [RelayCommand]
    public void RemoveRow(EmailRowVm? row)
    {
        if (row is null) return;

        Emails.Remove(row);
        if (Emails.Count == 0)
            Emails.Add(new EmailRowVm());
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var dl = Selected;
        if (dl is null) return;

        await _gate.WaitAsync();
        try
        {
            IsBusy = true;
            Status = "";

            var dlName = (Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(dlName))
            {
                Status = "DL Name is required.";
                return;
            }

            static bool LooksLikeEmail(string s)
                => s.Contains('@') && s.Contains('.') && s.Length <= 320;

            var cleaned = Emails
                .Select(x => new DistributionListEmailRow
                {
                    DisplayName = (x.DisplayName ?? "").Trim(),
                    Email = (x.Email ?? "").Trim()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .ToList();

            foreach (var e in cleaned)
            {
                if (!LooksLikeEmail(e.Email))
                {
                    Status = $"Invalid email: {e.Email}";
                    return;
                }
            }

            dl.Name = dlName;
            await _repo.UpsertAsync(dl, cleaned);

            // Refresh list order (name may have changed)
            var all = await _repo.GetAllAsync();
            Lists.Clear();
            foreach (var item in all) Lists.Add(item);

            Selected = Lists.FirstOrDefault(x => x.Id == dl.Id) ?? Lists.FirstOrDefault();

            Status = "Saved.";
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        var dl = Selected;
        if (dl is null) return;

        await _gate.WaitAsync();
        try
        {
            IsBusy = true;
            Status = "";

            await _repo.DeleteAsync(dl.Id);

            var all = await _repo.GetAllAsync();
            Lists.Clear();
            foreach (var item in all) Lists.Add(item);

            Selected = Lists.FirstOrDefault();

            Status = "Deleted.";
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }

        // If list became empty, create a new one OUTSIDE the gate (no deadlock)
        if (Lists.Count == 0)
            await NewAsync();
    }

    public sealed partial class EmailRowVm : ObservableObject
    {
        [ObservableProperty] private string displayName = "";
        [ObservableProperty] private string email = "";
    }
}

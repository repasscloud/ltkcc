// File: Views/DistributionListsPage.xaml.cs
using LTKCC.ViewModels;

namespace LTKCC.Views;

public partial class DistributionListsPage : ContentPage
{
    private readonly DistributionListsViewModel _vm;

    public DistributionListsPage(DistributionListsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Lists.Count == 0)
            await _vm.LoadAsync();
    }
}

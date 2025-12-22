using LTKCC.ViewModels;

namespace LTKCC.Views;

public partial class SupportedApplicationsPage : ContentPage
{
    private readonly SupportedApplicationsViewModel _vm;

    public SupportedApplicationsPage(SupportedApplicationsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}

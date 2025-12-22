using LTKCC.ViewModels;

namespace LTKCC.Views;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _vm;

    public ClientsPage(ClientsViewModel vm)
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

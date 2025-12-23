// File: Views/SendGridSettingsPage.xaml.cs
using LTKCC.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LTKCC.Views;

public partial class SendGridSettingsPage : ContentPage
{
    private readonly SendGridSettingsViewModel _vm;

    public SendGridSettingsPage() : this(App.Services.GetRequiredService<SendGridSettingsViewModel>())
    {
    }

    public SendGridSettingsPage(SendGridSettingsViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _vm.EnsureDbReadyAsync();
            await _vm.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            // Prevent a hard crash on navigation if something goes wrong.
            await DisplayAlert("SendGrid settings", ex.Message, "OK");
        }
    }
}

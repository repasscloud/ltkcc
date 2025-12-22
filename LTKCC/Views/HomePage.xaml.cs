namespace LTKCC.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnGoSettingsClicked(object sender, EventArgs e)
    {
        // "//settings" means "go to the root route named settings"
        await Shell.Current.GoToAsync("//settings");
    }
}

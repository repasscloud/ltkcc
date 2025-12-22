namespace LTKCC.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnGoMainClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main");
    }
}

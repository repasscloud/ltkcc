namespace LTKCC.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public static string BaseDir { get; } = Services.AppPaths.GetBaseDataDir();
}

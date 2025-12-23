using Microsoft.Maui.ApplicationModel;

namespace LTKCC.Services;

public sealed class AlertService : IAlertService
{
    public Task ShowAsync(string title, string message, string cancel = "OK")
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.MainPage;
            if (page is null) return;

            await page.DisplayAlert(title, message, cancel);
        });
    }
}

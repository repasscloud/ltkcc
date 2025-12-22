namespace LTKCC.Models;

public sealed class AppSettings
{
    public string MailSendApiUrl { get; set; } = "https://api.example.com";
    public string ApiUsername { get; set; } = "user";
    public string ApiPassword { get; set; } = "password";
}
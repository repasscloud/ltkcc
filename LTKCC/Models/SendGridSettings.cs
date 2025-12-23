using SQLite;

namespace LTKCC.Models;

public sealed class SendGridSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string ApiUri { get; set; } = "https://api.sendgrid.com/v3/mail/send";

    // Stored encrypted in DB
    public string KeyName { get; set; } = "SG.API.Key";

    // Stored encrypted in DB
    public string ApiKey { get; set; } = "YOUR_SENDGRID_API_KEY";

    public string FromEmail { get; set; } = "no-reply@www.com";
    public string FromName { get; set; } = "LTKCC";
}

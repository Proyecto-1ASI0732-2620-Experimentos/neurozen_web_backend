namespace neurozen.API.IAM.Infrastructure.Email;

/// <summary>
///     Bound from the "Email" section. SMTP credentials go in environment variables
///     (Email__Host, Email__Username, Email__Password, ...).
///     In development, PickupDirectory writes each email as an .eml file instead of sending it.
/// </summary>
public class EmailSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@neurozen.local";
    public string FromName { get; set; } = "NeuroZen";
    public string? PickupDirectory { get; set; }
}

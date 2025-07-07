public class SmtpConfig
{
    public string Host { get; set; } = null!;
    public int Port { get; set; } 
    public bool EnableSsl { get; set; } 
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AlertSettings
{
    public string DestinationEmail { get; set; } = null!;
    public SmtpConfig Smtp { get; set; } = null!;
}
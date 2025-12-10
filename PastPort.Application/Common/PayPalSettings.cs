namespace PastPort.Application.Common;

public class PayPalSettings
{
    public string Mode { get; set; } = "sandbox";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
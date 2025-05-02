using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required(ErrorMessage = "Email or Username is required.")]
    public string EmailOrUsername { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }

    public DeviceDetails? Device { get; set; }
}

public class DeviceDetails
{
    public string? Ip { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OS { get; set; }
}

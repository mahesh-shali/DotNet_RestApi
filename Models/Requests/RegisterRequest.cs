using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RestApi.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required.")]
    public string name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    // [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    // [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string password { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    public string phone { get; set; }

    public int? roleId { get; set; }

    [JsonPropertyName("selectedCode")]    //change it to selectedCountry when api limit backs feb 1
    public string? phonePrefix { get; set; } = " ";

    [JsonPropertyName("ip")]
    public string? ipAddress { get; set; }

    [JsonPropertyName("browserInfo")]
    public BrowserInfo? browserInfo { get; set; }

    [JsonPropertyName("osName")]
    public string? os { get; set; }

    public int? organizationId { get; set; }
}

public class BrowserInfo
{
    [JsonPropertyName("browserName")]
    public string? Browser { get; set; }

    [JsonPropertyName("browserVersion")]
    public string? BrowserVersion { get; set; }
}

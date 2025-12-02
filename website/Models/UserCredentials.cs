using System.Text.Json.Serialization;

namespace website.Models;

public class UserCredentials
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

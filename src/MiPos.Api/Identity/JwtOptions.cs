namespace MiPos.Api.Identity;

public class JwtOptions
{
    public string Issuer { get; set; } = "MiPos";
    public string Audience { get; set; } = "MiPos.Clients";
    public string SigningKey { get; set; } = "";
    public int AccessTokenHours { get; set; } = 12;
}

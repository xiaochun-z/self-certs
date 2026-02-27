namespace SelfCerts.Api.Models;

public class CaConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CaCrt { get; set; } = string.Empty;
    public string CaKey { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
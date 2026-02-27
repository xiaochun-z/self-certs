namespace SelfCerts.Api.Models;

public class CertRecord
{
    public Guid Id { get; set; }

    public int CaConfigId { get; set; }

    public string ServerReqCnf { get; set; } = string.Empty;
    
    public string ServerKey { get; set; } = string.Empty;
    
    public string ServerCrt { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
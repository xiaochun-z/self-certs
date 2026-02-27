namespace SelfCerts.Api.Models;

public class CreateCaRequest
{
    public required string Name { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class GenerateCertRequest
{
    public int CaId { get; set; }
    public string CaPassword { get; set; } = string.Empty;
    public required string ServerReqCnfTemplate { get; set; }
}

public class GenerateCertResponse
{
    public required string ServerKey { get; set; }
    public required string ServerCrt { get; set; }
}

public class CaConfigResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CaCrt { get; set; } = string.Empty;
    public string CaKey { get; set; } = string.Empty;
}

public class CertRecordResponse
{
    public Guid Id { get; set; }
    public int CaConfigId { get; set; }
    public string ServerReqCnf { get; set; } = string.Empty;
    public string ServerKey { get; set; } = string.Empty;
    public string ServerCrt { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
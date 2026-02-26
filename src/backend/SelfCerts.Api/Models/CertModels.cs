namespace SelfCerts.Api.Models;

public class GenerateCertRequest
{
    public required string CaCrt { get; set; }
    public required string CaKey { get; set; }
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
    public string CaCrt { get; set; } = string.Empty;
    public string CaKey { get; set; } = string.Empty;
}
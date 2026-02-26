using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfCerts.Api.Infrastructure;
using SelfCerts.Api.Models;
using SelfCerts.Api.Services;

namespace SelfCerts.Api.Controllers;

public class CertController : ApiControllerBase
{
    private readonly OpenSslService _openSslService;
    private readonly SelfCertsDbContext _dbContext;

    public CertController(OpenSslService openSslService, SelfCertsDbContext dbContext)
    {
        _openSslService = openSslService;
        _dbContext = dbContext;
    }

    [HttpGet("ca")]
    public async Task<ActionResult<ApiResult<CaConfigResponse>>> GetCaConfig()
    {
        var config = await _dbContext.CaConfigs.FirstOrDefaultAsync();
        if (config == null) return Success(new CaConfigResponse());
        return Success(new CaConfigResponse { CaCrt = config.CaCrt, CaKey = config.CaKey });
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResult<GenerateCertResponse>>> GenerateCert([FromBody] GenerateCertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CaCrt) || string.IsNullOrWhiteSpace(request.CaKey))
        {
            return Error<GenerateCertResponse>("CA Certificate and Private Key are required.");
        }

        var (key, crt) = await _openSslService.GenerateServerCertAsync(
            request.CaCrt,
            request.CaKey,
            request.CaPassword,
            request.ServerReqCnfTemplate);

        var record = new CertRecord
        {
            Id = Guid.NewGuid(),
            ServerReqCnf = request.ServerReqCnfTemplate,
            ServerKey = key,
            ServerCrt = crt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.CertRecords.Add(record);

        var caConfig = await _dbContext.CaConfigs.FirstOrDefaultAsync();
        if (caConfig == null)
        {
            _dbContext.CaConfigs.Add(new CaConfig { CaCrt = request.CaCrt, CaKey = request.CaKey });
        }
        else
        {
            caConfig.CaCrt = request.CaCrt;
            caConfig.CaKey = request.CaKey;
            caConfig.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        return Success(new GenerateCertResponse
        {
            ServerKey = key,
            ServerCrt = crt
        });
    }
}
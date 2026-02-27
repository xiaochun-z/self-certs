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

    [HttpGet("cas")]
    public async Task<ActionResult<ApiResult<List<CaConfigResponse>>>> GetCas()
    {
        var configs = await _dbContext.CaConfigs.OrderByDescending(c => c.UpdatedAt).ToListAsync();
        return Success(configs.Select(c => new CaConfigResponse { Id = c.Id, Name = c.Name, CaCrt = c.CaCrt, CaKey = c.CaKey }).ToList());
    }

    [HttpPost("ca/import")]
    public async Task<ActionResult<ApiResult<CaConfigResponse>>> ImportCa([FromBody] ImportCaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Error<CaConfigResponse>("CA Name is required.");
        if (string.IsNullOrWhiteSpace(request.CaCrt)) return Error<CaConfigResponse>("CA Certificate is required.");
        if (string.IsNullOrWhiteSpace(request.CaKey)) return Error<CaConfigResponse>("CA Private Key is required.");

        var caConfig = new CaConfig
        {
            Name = request.Name,
            CaCrt = request.CaCrt,
            CaKey = request.CaKey,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.CaConfigs.Add(caConfig);
        await _dbContext.SaveChangesAsync();

        return Success(new CaConfigResponse { Id = caConfig.Id, Name = caConfig.Name, CaCrt = caConfig.CaCrt, CaKey = caConfig.CaKey });
    }

    [HttpPost("ca")]
    public async Task<ActionResult<ApiResult<CaConfigResponse>>> CreateCa([FromBody] CreateCaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error<CaConfigResponse>("CA Name is required.");

        var (key, crt) = await _openSslService.GenerateCaCertAsync(request.Name, request.Password);

        var caConfig = new CaConfig
        {
            Name = request.Name,
            CaCrt = crt,
            CaKey = key,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.CaConfigs.Add(caConfig);
        await _dbContext.SaveChangesAsync();

        return Success(new CaConfigResponse { Id = caConfig.Id, Name = caConfig.Name, CaCrt = caConfig.CaCrt, CaKey = caConfig.CaKey });
    }

    [HttpGet("history/{caId}")]
    public async Task<ActionResult<ApiResult<List<CertRecordResponse>>>> GetHistory(int caId)
    {
        var records = await _dbContext.CertRecords
            .Where(r => r.CaConfigId == caId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Success(records.Select(r => new CertRecordResponse
        {
            Id = r.Id,
            CaConfigId = r.CaConfigId,
            ServerReqCnf = r.ServerReqCnf,
            ServerKey = r.ServerKey,
            ServerCrt = r.ServerCrt,
            CreatedAt = r.CreatedAt
        }).ToList());
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResult<GenerateCertResponse>>> GenerateCert([FromBody] GenerateCertRequest request)
    {
        var caConfig = await _dbContext.CaConfigs.FindAsync(request.CaId);
        if (caConfig == null) return Error<GenerateCertResponse>("CA not found.");

        if (string.IsNullOrWhiteSpace(request.ServerReqCnfTemplate))
            return Error<GenerateCertResponse>("Server Request Config is required.");

        var (key, crt) = await _openSslService.GenerateServerCertAsync(
            caConfig.CaCrt,
            caConfig.CaKey,
            request.CaPassword,
            request.ServerReqCnfTemplate);

        var record = new CertRecord
        {
            Id = Guid.NewGuid(),
            CaConfigId = caConfig.Id,
            ServerReqCnf = request.ServerReqCnfTemplate,
            ServerKey = key,
            ServerCrt = crt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.CertRecords.Add(record);
        await _dbContext.SaveChangesAsync();

        return Success(new GenerateCertResponse
        {
            ServerKey = key,
            ServerCrt = crt
        });
    }
}
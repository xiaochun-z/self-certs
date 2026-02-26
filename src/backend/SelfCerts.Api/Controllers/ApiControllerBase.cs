using SelfCerts.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace SelfCerts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase 
{
    protected ActionResult<ApiResult<T>> Success<T>(T data, string message = "Success")
    {
        return Ok(ApiResult<T>.Success(data, message));
    }

    protected ActionResult<ApiResult> Success(string message = "Success")
    {
        return Ok(ApiResult.Success(message));
    }

    protected ActionResult<ApiResult> Error(string message, int code = 400)
    {
        return BadRequest(ApiResult.Error(code, message));
    }

    protected ActionResult<ApiResult<T>> Error<T>(string message, int code = 400)
    {
        return BadRequest(ApiResult<T>.Error(code, message));
    }
}
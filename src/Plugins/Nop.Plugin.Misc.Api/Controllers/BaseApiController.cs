using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.Api.DTOs;
using Nop.Plugin.Misc.Api.Filters;

namespace Nop.Plugin.Misc.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ServiceFilter(typeof(ApiAuthorizationFilter))]
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult OkResponse<T>(T data, string message = null)
    {
        var response = new BaseApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
        return Ok(response);
    }

    protected IActionResult BadRequestResponse(string message)
    {
        var response = new BaseApiResponse<string>
        {
            Success = false,
            Message = message
        };
        return BadRequest(response);
    }

    protected IActionResult ErrorResponse(string message)
    {
        var response = new BaseApiResponse<string>
        {
            Success = false,
            Message = message
        };
        return StatusCode(500, response);
    }
}


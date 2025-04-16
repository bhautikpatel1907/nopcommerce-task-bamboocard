using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Plugin.Misc.Api.DTOs;

namespace Nop.Plugin.Misc.Api.Filters;

public class ApiAuthorizationFilter : IAsyncAuthorizationFilter
{
    #region Fields

    private readonly ApiSettings _settings;

    #endregion

    #region Ctor

    public ApiAuthorizationFilter(ApiSettings settings)
    {
        _settings = settings;
    }

    #endregion

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_settings.Enable)
        {
            context.Result = new JsonResult(new BaseApiResponse<string>
            {
                Success = false,
                Message = "API is currently disabled."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return Task.CompletedTask;
        }

        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? false)
        {
            context.Result = new JsonResult(new BaseApiResponse<string>
            {
                Success = false,
                Message = "Unauthorized request."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        return Task.CompletedTask;
    }
}


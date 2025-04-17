using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Plugin.Misc.Api.DTOs;
using Nop.Services.Customers;

namespace Nop.Plugin.Misc.Api.Filters;

public class ApiAuthorizationFilter : IAsyncAuthorizationFilter
{
    #region Fields

    private readonly ApiSettings _settings;
    private readonly ICustomerService _customerService;

    #endregion

    #region Ctor

    public ApiAuthorizationFilter(ApiSettings settings,
        ICustomerService customerService)
    {
        _settings = settings;
        _customerService = customerService;
    }

    #endregion

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
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
            return;
        }

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? false)
        {
            context.Result = new JsonResult(new BaseApiResponse<string>
            {
                Success = false,
                Message = "Unauthorized request."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // Extract username from token
        var username = user.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            context.Result = new JsonResult(new BaseApiResponse<string>
            {
                Success = false,
                Message = "Username is missing in token."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        //get customer
        var customer = await _customerService.GetCustomerByUsernameAsync(username);
        if (customer == null || !customer.Active || customer.Deleted)
        {
            context.Result = new JsonResult(new BaseApiResponse<string>
            {
                Success = false,
                Message = "Invalid or inactive customer."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        return;
    }
}


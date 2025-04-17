using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.Api.Controllers;

[Route("api/token")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IStoreContext _storeContext;
    private readonly ICustomerService _customerService;
    private readonly ICustomerRegistrationService _customerRegistrationService;
    private readonly ILocalizationService _localizationService;

    public AuthApiController(IStoreContext storeContext,
        ICustomerService customerService,
        ICustomerRegistrationService customerRegistrationService,
        ILocalizationService localizationService)
    {
        _storeContext = storeContext;
        _customerService = customerService;
        _customerRegistrationService = customerRegistrationService;
        _localizationService = localizationService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> GetToken([FromForm] string username, [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Please enter username" });
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Please enter password" });

        // Validate the customer credentials
        var loginResult = await _customerRegistrationService.ValidateCustomerAsync(username, password);
        switch (loginResult)
        {
            case CustomerLoginResults.Successful:
                {
                    var customer = await _customerService.GetCustomerByUsernameAsync(username);
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiDefaults.ApiKey));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                    var store = await _storeContext.GetCurrentStoreAsync();

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                        new Claim(ClaimTypes.Name, customer.Username ?? customer.Email),
                    };

                    var token = new JwtSecurityToken(
                        issuer: "yourdomain.com", //enter you store domain, I'm keeping it to static for demo purpose
                        audience: "yourdomain.com", //enter you store domain, I'm keeping it to static for demo purpose
                        claims: claims,
                        expires: DateTime.UtcNow.AddHours(1),
                        signingCredentials: credentials
                    );

                    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                    return await Task.FromResult(Ok(new { token = tokenString }));
                }
            case CustomerLoginResults.MultiFactorAuthenticationRequired:
                return Unauthorized(new { message = "MultiFactorVerification is required" });

            case CustomerLoginResults.CustomerNotExist:
                return NotFound(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist") });

            case CustomerLoginResults.Deleted:
                return NotFound(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted") });

            case CustomerLoginResults.NotActive:
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive") });

            case CustomerLoginResults.NotRegistered:
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered") });

            case CustomerLoginResults.LockedOut:
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut") });

            case CustomerLoginResults.WrongPassword:
            default:
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Account.Login.WrongCredentials") });
        }
    }
}

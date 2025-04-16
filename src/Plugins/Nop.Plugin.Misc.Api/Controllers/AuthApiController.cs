using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nop.Core;

namespace Nop.Plugin.Misc.Api.Controllers;

[Route("api/token")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly IStoreContext _storeContext;

    public AuthApiController(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    [AllowAnonymous]
    public async Task<IActionResult> GetToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiDefaults.ApiKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var store = await _storeContext.GetCurrentStoreAsync();

        var token = new JwtSecurityToken(
            issuer: store.Url,
            audience: store.Url,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return await Task.FromResult(Ok(new { token = tokenString }));
    }
}

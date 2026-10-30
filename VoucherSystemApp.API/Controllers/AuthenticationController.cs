using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoucherSystemApp.API.Authentication;

namespace VoucherSystemApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(IOptions<PosAuthenticationOptions> options) : ControllerBase
{
    public sealed class AuthenticationData
    {
        [Required, StringLength(64)] public string UserName { get; set; } = "";
        [Required, StringLength(256, MinimumLength = 32)] public string Password { get; set; } = "";
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting("token")]
    public IActionResult Authenticate(AuthenticationData data)
    {
        var settings = options.Value;
        var client = settings.Clients.SingleOrDefault(c => c.ClientId == data.UserName);
        if (client is null || !client.Matches(data.Password)) return Unauthorized();

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.ClientId),
            new("till_id", client.TillId),
            new("outlet_id", client.OutletId.ToString(CultureInfo.InvariantCulture)),
            new("department", client.DepartmentType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            now, now.AddMinutes(10), new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        Response.Headers.CacheControl = "no-store";
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer", expiresIn = 600 });
    }
}

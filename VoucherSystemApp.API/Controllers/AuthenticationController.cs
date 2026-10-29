using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace VoucherSystemApp.API.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly IConfiguration config;

        public AuthenticationController(IConfiguration config)
        {
            this.config = config;
        }
        public record AuthenticationData(string? UserName, string? password);
        public record UserData(int Id, string FirstName, string LastName, string UserName);
        [HttpPost("token")]
        [AllowAnonymous]
        public ActionResult<string> Authenticate([FromBody] AuthenticationData data)
        {
            var user = ValidateCredentials(data);

            if (user == null)
            {
                return Unauthorized();
            }

            var token = GenerateToken(user);
            return Ok(token);
        }

        private object GenerateToken(UserData user)
        {
            var secreteKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.GetValue<string>("Authentication: SecreteKey")!));

            var signingCreedentials = new SigningCredentials(secreteKey, SecurityAlgorithms.HmacSha256);

            List<Claim> claims = new();
            claims.Add(new(JwtRegisteredClaimNames.Sub, user.Id.ToString()));
            claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName));

            var token = new JwtSecurityToken(config.GetValue<string>("Authentication:Issuer"),
                config.GetValue<string>("Authentication:Audience"),
                claims,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(10),
                signingCreedentials);
        }
        private UserData? ValidateCredentials(AuthenticationData data)
        {
            if CompareValues() {

            }
            return null;
        private bool CompareValues(string? actual, string? expected)
        {
            if (actual != null)
            {
                if (actual.Equals(expected))
                {
                    return true;
                }
            }
            return false;
        }
    }

        private bool CompareValues(AuthenticationData data)
        {
            throw new NotImplementedException();
        }
    }

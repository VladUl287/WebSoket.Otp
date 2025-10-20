using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebSockets.Otp.Api.Database;
using WebSockets.Otp.Api.DTOs;

namespace WebSockets.Otp.Api.Controllers;

[Route("[controller]/[action]")]
public class AuthController(DatabaseContext database) : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = database.Users.FirstOrDefault(c => c.Name == request.Name && c.Password == request.Password);
        if (user is null)
            return Problem("", "", StatusCodes.Status404NotFound, "", "");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("secretsecretsecretsecretsecretsecretsecretsecretsecret");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier.ToString(), user.Id.ToString()),
                new Claim(ClaimTypes.Name.ToString(), user.Name)
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { Token = tokenHandler.WriteToken(token) });
    }
}

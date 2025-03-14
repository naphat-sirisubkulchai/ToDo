using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ToDo.Models;
using ToDo.DTOs;

namespace ToDo.Controllers;

[ApiController]
[Route("[controller]")]
public class TokensController : ControllerBase
{
    private readonly ILogger<TokensController> _logger;

    public TokensController(ILogger<TokensController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Post([FromBody] Login data)
    {
        var db = new ToDoDbContext();

        var user = (from x in db.User
                    where x.NationalId == data.NationalId
                    select x).FirstOrDefault();
        if (user == null || user.Salt == null)
        {
            return BadRequest("Invalid user credentials.");
        }

        string hash = Convert.ToBase64String(
            KeyDerivation.Pbkdf2(
                password: data.Password,
                salt: Convert.FromBase64String(user.Salt),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 32
            )
        );
        if (hash != user.HashedPassword)
        {
            return Unauthorized("Invalid user credentials.");
        }

        var desc = new SecurityTokenDescriptor();
        desc.Subject = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "user")
        });
        desc.NotBefore = DateTime.UtcNow;
        desc.Expires = DateTime.UtcNow.AddHours(3);
        desc.IssuedAt = DateTime.UtcNow;
        desc.Issuer = "ToDo";
        desc.Audience = "ToDo";
        desc.SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Program.SecurityKey)),
            SecurityAlgorithms.HmacSha256Signature
        );
        var hanlder = new JwtSecurityTokenHandler();
        var token = hanlder.CreateToken(desc);
        return Ok(new { token = hanlder.WriteToken(token) });
    }
}
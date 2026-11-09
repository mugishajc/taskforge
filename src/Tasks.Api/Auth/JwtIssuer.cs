using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tasks.Api.Auth;

public interface IJwtIssuer
{
    string IssueAccessToken(int userId, string email);
}

public class JwtIssuer(IConfiguration config) : IJwtIssuer
{
    public string IssueAccessToken(int userId, string email)
    {
        var jwt = config.GetSection("Jwt");
        var keyText = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = int.Parse(jwt["AccessTokenMinutes"] ?? "60");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using Microsoft.IdentityModel.Tokens;
using SharedClassLibrary.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace SharedClassLibrary.Helpers;

public static class JwtHelper
{
    public static ClaimsPrincipal SetClaimPrincipal(UserSession model)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
            new(ClaimTypes.NameIdentifier, model.Id!),
            new(ClaimTypes.Name, model.Name!),
            new(ClaimTypes.Email, model.Email!),
            new(ClaimTypes.Role, model.Role!),
            ], "JwtAuth"));
    }

    public static string GenerateJwtToken(UserSession model, string jwtKey, string issuer, string audience)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(jwtKey));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims:
            [
            new(ClaimTypes.NameIdentifier, model.Id!),
            new(ClaimTypes.Name, model.Name!),
            new(ClaimTypes.Email, model.Email!),
            new(ClaimTypes.Role, model.Role!),
            ],
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static UserSession GetClaimsFromToken(string jwtToken)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = handler.ReadJwtToken(jwtToken);
        IEnumerable<Claim> claims = token.Claims;

        string Id = claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value!;
        string Name = claims.First(c => c.Type == ClaimTypes.Name).Value!;
        string Email = claims.First(c => c.Type == ClaimTypes.Email).Value!;
        string Role = claims.First(c => c.Type == ClaimTypes.Role).Value!;

        return new UserSession(Id, Name, Email, Role);
    }
}

using Microsoft.IdentityModel.Tokens;
using SharedClassLibrary.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedClassLibrary.GenericModels
{
    public static class Generics
    {
        public static ClaimsPrincipal SetClaimPrincipal(UserSession model)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, model.Id!),
                    new(ClaimTypes.Name, model.Name!),
                    new(ClaimTypes.Email, model.Email!),
                    new(ClaimTypes.Role, model.Role!),
                }, "JwtAuth"));
        }

        public static string GenerateJwtToken(UserSession model, string jwtKey, string issuer, string audience)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, model.Id!),
                    new(ClaimTypes.Name, model.Name!),
                    new(ClaimTypes.Email, model.Email!),
                    new(ClaimTypes.Role, model.Role!),
                },
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static UserSession GetClaimsFromToken(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            var claims = token.Claims;

            string Id = claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value!;
            string Name = claims.First(c => c.Type == ClaimTypes.Name).Value!;
            string Email = claims.First(c => c.Type == ClaimTypes.Email).Value!;
            string Role = claims.First(c => c.Type == ClaimTypes.Role).Value!;

            return new UserSession(Id, Name, Email, Role);
        }

        public static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
            };
        }
        public static string SerializeObj<T>(T modelObject) => JsonSerializer.Serialize(modelObject, JsonOptions());
        public static T DeserializeJsonString<T>(string jsonString) => JsonSerializer.Deserialize<T>(jsonString, JsonOptions())!;
        public static IList<T> DeserializeJsonStringList<T>(string jsonString) => JsonSerializer.Deserialize<IList<T>>(jsonString, JsonOptions())!;

        public static StringContent GenerateStringContent(string serialiazedObj) => new(serialiazedObj, System.Text.Encoding.UTF8, "application/json");
    }
}

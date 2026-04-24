using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AspNetTemplate.Data;
using AspNetTemplate.Core.Infra.Extensions;

namespace AspNetTemplate.Core.Infra.Helpers;

public static class JwtToken
{
    private static string _issuer = AppState.App_domain;
    private static string _secretKey = AppState.JwtSecretKey;

    public static (string token, string jti, DateTime exp) GenToken(string userId, string role, int days)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            throw new ArgumentException("JWT secret key is missing.", nameof(_secretKey));

        if (string.IsNullOrWhiteSpace(_issuer))
            throw new ArgumentException("JWT issuer is missing.", nameof(_issuer));

        if (Encoding.UTF8.GetByteCount(_secretKey) < 64)
            throw new InvalidOperationException("JWT secret key must be at least 64 bytes.");

        string jti = GuidV7.NewGuid().ToString();
        DateTime exp = DateTime.UtcNow.AddDays(days);

        byte[] bytes = Encoding.UTF8.GetBytes(_secretKey);
        JwtSecurityTokenHandler? jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        SecurityTokenDescriptor? tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _issuer,
            Subject = new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, jti) // 👈 unique ID
            }),
            Expires = exp,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(bytes),
                SecurityAlgorithms.HmacSha512Signature)
        };

        SecurityToken token = jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
        return (jwtSecurityTokenHandler.WriteToken(token), jti, exp);
    }

    public static ClaimsPrincipal? Validate(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey); // use the same key as GenToken

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                // Match what GenToken produced
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };

            return tokenHandler.ValidateToken(token, validationParams, out _);
        }
        catch (Exception)
        {
            return null;
        }
    }

}

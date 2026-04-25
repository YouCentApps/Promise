using System.Text;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Promise.Api.Helpers;

internal static class Security
{
    public const string AuthorizationHttpHeader = "Authorization";
    public const string PayLoadFieldLogin = "login";
    public const string PayLoadFieldAuth = "auth";
    public const string PayLoadFieldExp = "exp";

    private const int accessTokenLifetimeHours = 24;
    private const string bearerTokenPrefix = "Bearer ";
    private const int saltLengthLimit = 20; // 20 Bytes! (less than 32 characters after Base64 conversion)

    public static string GetHash(string input)
    {
        using var crypto = SHA256.Create();
        return GetCryptoHash(crypto, input);
    }


    public static double GetAccessTokenLifetimeSeconds()
    {
        var expiryTime = DateTime.UtcNow.AddHours(accessTokenLifetimeHours);
        return new DateTimeOffset(expiryTime).ToUnixTimeSeconds();
    }

    public static string CreateBearerJwt(IDictionary<string, object> payload, string secret)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = payload.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)).ToList();

        var expiryTime = payload.TryGetValue(PayLoadFieldExp, out var expValue)
            ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(expValue, System.Globalization.CultureInfo.InvariantCulture)).UtcDateTime
            : DateTime.UtcNow.AddHours(accessTokenLifetimeHours);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiryTime,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return bearerTokenPrefix + tokenString;
    }

    public static bool ValidateBearerJwtPrefix(string bearerJwt)
    {
        if (bearerJwt is null) return false;
        return bearerJwt.LastIndexOf(bearerTokenPrefix, StringComparison.Ordinal) == 0;
    }

    public static IDictionary<string, object> GetBearerJwtLoad(string bearerJwt, string secret, bool verify = true)
    {
        var jwt = GetJwtFromBearerJwt(bearerJwt);
        var payload = GetJwtLoad(jwt, secret, verify);
        return payload;
    }

    public static bool ValidateBearerAccessToken(string bearerJwt, string login, string secret)
    {
        if (!ValidateBearerJwtPrefix(bearerJwt)) return false;
        var jwt = GetJwtFromBearerJwt(bearerJwt);
        var payload = GetJwtLoad(jwt, secret, true);
        if (payload.Count == 0) return false;
        if (!payload.TryGetValue(PayLoadFieldLogin, out var loginValue)) return false;
        if (!payload.TryGetValue(PayLoadFieldAuth, out var authValue)) return false;
        return login.Equals(loginValue?.ToString(), StringComparison.OrdinalIgnoreCase)
               && authValue?.ToString() == true.ToString();
    }
    public static string GetPasswordHash(string password, string salt)
    {
        return GetHash(GetHash(password) + salt);
    }

    private static Dictionary<string, object> GetJwtLoad(string jwt, string secret, bool verify)
    {
        Dictionary<string, object> payload = [];
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

#pragma warning disable CA5404
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = verify,
                ClockSkew = TimeSpan.Zero
            };
#pragma warning restore CA5404

            var principal = tokenHandler.ValidateToken(jwt, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                foreach (var claim in jwtToken.Claims)
                {
                    payload[claim.Type] = claim.Value;
                }
            }
        }
        catch (SecurityTokenExpiredException)
        {
            MainLogger.Log("Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            MainLogger.Log("Token validation failed: " + ex.Message);
        }
        catch (InvalidOperationException e)
        {
            MainLogger.Log("JWT exception " + e.Message);
        }
        return payload;
    }

    private static string GetJwtFromBearerJwt(string bearerJwt)
    {
        return bearerJwt[bearerTokenPrefix.Length..];
    }

    private static string GetCryptoHash(HashAlgorithm crypto, string input)
    {
        var bytes = crypto.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sBuilder = new StringBuilder();
        foreach (var b in bytes)
        {
            sBuilder.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }
        return sBuilder.ToString();
    }




    public static string GetSalt()
    {
        return GetSalt(saltLengthLimit);
    }
    private static string GetSalt(int maximumSaltLength)
    {
        var salt = new byte[maximumSaltLength];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToBase64String(salt);
    }


    internal static string GetMaskedEmail(string email)
    {
        var emailParts = email.Split('@');
        if (emailParts.Length != 2) return "*not*valid*email";
        var localPart = emailParts[0];
        var domainPart = emailParts[1];
        var maskedLocalPart = localPart[..2] + "****" + localPart[^2..];
        var domainParts = domainPart.Split('.');
        if (domainParts.Length < 2) return "*not*valid*email";
        var maskedDomainPart = domainParts[0][..2] + "****" + domainParts[0][^2..] + "." + string.Join('.', domainParts.Skip(1));
        var maskedEmail = maskedLocalPart + "@" + maskedDomainPart;
        return maskedEmail;
    }

    internal static string GetMaskedTel(string tel)
    {
        if (string.IsNullOrEmpty(tel) || tel.Length < 4) return "*not*valid*tel";
        var maskedTel = new string('*', tel.Length - 4) + tel[^4..];
        return maskedTel;
    }

    internal static string GenerateTemporaryPassword()
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRS0123456789TUVWXYZ0123456789abcdefghijklmnopqrs0123456789tuvwxyz0123456789";
        var result = new char[8];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(result);
    }

}

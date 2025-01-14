﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class AuthService
{
    private readonly string _jwtKey;

    public AuthService(string jwtKey)
    {
        _jwtKey = jwtKey;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string? GetUserIdFromToken(string authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        try
        {
            var token = authorizationHeader.Replace("Bearer ", string.Empty);
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return null;

            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "nameid");

            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decoding token: {ex.Message}");
            return null;
        }
    }


    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, user.Nickname),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())

        }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

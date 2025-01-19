using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace hundir_la_flota.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(User user);
        int? GetUserIdFromToken(string authorizationHeader);
    }


    public class AuthService : IAuthService
    {
        private readonly string _jwtKey;
        private readonly ILogger<AuthService> _logger;

        private const string ClaimId = "id";
        private const string ClaimNickname = "nickname";
        private const string ClaimEmail = "email";

        public AuthService(string jwtKey, ILogger<AuthService> logger)
        {
            _jwtKey = jwtKey;
            _logger = logger;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public string GenerateJwtToken(User user)
        {
            _logger.LogInformation($"Generando token para el usuario: {user.Nickname}");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var claims = new[]
            {
                new Claim(ClaimId, user.Id.ToString()),
                new Claim(ClaimNickname, user.Nickname),
                new Claim(ClaimEmail, user.Email)
            };

            _logger.LogDebug("Claims utilizados en el token: {@Claims}", claims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("Token generado exitosamente.");
            return tokenHandler.WriteToken(token);
        }

        public int? GetUserIdFromToken(string authorizationHeader)
        {
            var token = authorizationHeader?.Replace("Bearer ", string.Empty);
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Encabezado de autorización vacío o nulo.");
                return null;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("El token no se puede leer.");
                    return null;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                handler.ValidateToken(token, validationParameters, out _);
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                var userIdClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimId);
                if (userIdClaim == null)
                {
                    _logger.LogWarning($"El claim '{ClaimId}' no se encontró en el token.");
                    return null;
                }

                if (int.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogInformation($"ID de usuario obtenido del token: {userId}");
                    return userId;
                }
                else
                {
                    _logger.LogWarning($"No se pudo convertir el claim '{ClaimId}' a int: {userIdClaim.Value}");
                    return null;
                }
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError($"Error de seguridad al validar el token: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al procesar el token: {ex.Message}");
                return null;
            }
        }

    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtService(
            IOptions<JwtSettings> jwtSettings
        )
        {
            _jwtSettings = jwtSettings.Value;
        }

        /*
         =========================================
         GENERATE ADMIN TOKEN
         =========================================
        */

        public string GenerateAdminToken(
            Admin admin
        )
        {
            var tokenHandler =
                new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(
                _jwtSettings.Secret
            );

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    admin.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Email,
                    admin.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    "Admin"
                )
            };

            var tokenDescriptor =
                new SecurityTokenDescriptor
                {
                    Subject =
                        new ClaimsIdentity(claims),

                    Expires =
                        DateTime.UtcNow.AddMinutes(
                            _jwtSettings.ExpiryMinutes
                        ),

                    Issuer =
                        _jwtSettings.Issuer,

                    Audience =
                        _jwtSettings.Audience,

                    SigningCredentials =
                        new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha256Signature
                        )
                };

            var token =
                tokenHandler.CreateToken(
                    tokenDescriptor
                );

            return tokenHandler.WriteToken(
                token
            );
        }

        /*
         =========================================
         GENERATE USER TOKEN
         =========================================
        */

        public string GenerateToken(
            string userId,
            string email,
            string role
        )
        {
            var tokenHandler =
                new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(
                _jwtSettings.Secret
            );

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId
                ),

                new Claim(
                    ClaimTypes.Email,
                    email
                ),

                new Claim(
                    ClaimTypes.Role,
                    role
                )
            };

            var tokenDescriptor =
                new SecurityTokenDescriptor
                {
                    Subject =
                        new ClaimsIdentity(claims),

                    Expires =
                        DateTime.UtcNow.AddMinutes(
                            _jwtSettings.ExpiryMinutes
                        ),

                    Issuer =
                        _jwtSettings.Issuer,

                    Audience =
                        _jwtSettings.Audience,

                    SigningCredentials =
                        new SigningCredentials(
                            new SymmetricSecurityKey(key),
                            SecurityAlgorithms.HmacSha256Signature
                        )
                };

            var token =
                tokenHandler.CreateToken(
                    tokenDescriptor
                );

            return tokenHandler.WriteToken(
                token
            );
        }
    }
}
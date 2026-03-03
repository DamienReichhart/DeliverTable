using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DeliverTableServer.Configuration;
using DeliverTableServer.Models;
using DeliverTableServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace DeliverTableServer.Services
{
    public class TokenService(JwtConfig jwtConfig, UserManager<User> userManager) : ITokenService
    {
        private readonly JwtConfig _jwtConfig = jwtConfig;
        private readonly UserManager<User> _userManager = userManager;
        private readonly string _defaultRoleValue = "Customer";

        public async Task<string> CreateToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_jwtConfig.Key);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault(_defaultRoleValue);

            List<Claim> claims = [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, role)
        ];

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpireMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
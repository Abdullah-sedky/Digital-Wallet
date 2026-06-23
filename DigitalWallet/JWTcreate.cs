using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DigitalWallet.Models;

namespace DigitalWallet
{
    public class JWTcreate
    {
        private readonly IConfiguration _config;
        public JWTcreate(IConfiguration config)
        {
            _config = config;
        }
        public string createToken(User user)
        {
         var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var secret = _config["Jwt:SecretKey"]; 
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var signingCreds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "DigitalWallet",
                audience: "DigitalWalletUsers",
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: signingCreds,
                claims: claims
                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }
}

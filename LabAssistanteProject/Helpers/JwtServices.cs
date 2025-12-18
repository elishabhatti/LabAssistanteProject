using LabAssistanteProject.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LabAssistanteProject.Helpers
{

    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Users user)
        {
            var claims = new[]
       {
    new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
    new Claim("UserId", user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username ?? ""),
    new Claim("email", user.Email!) // add email claim for HomeController
};


            var keyString = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Configuration Error: 'Jwt:Key' is missing or null.");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyString)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NKZAPI.Models;

namespace NKZAPI.Services.PassService
{
    public class PasswordService : IPasswordInterface
    {
        private readonly IConfiguration _configuration;
        public PasswordService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void CreatePassHash(string Pass, out byte[] PassHash, out byte[] PassSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                PassSalt = hmac.Key;
                PassHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Pass));
            }
        }
        public bool VerifyPassHash(string Pass, byte[] PassHash, byte[] PassSalt)
        {
            using (var hmac = new HMACSHA512(PassSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Pass));
                return computedHash.SequenceEqual(PassHash);
            }


        }
        public string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("Email", user.Email),
                new Claim("Player", System.Text.Json.JsonSerializer.Serialize(user.Player))

            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}

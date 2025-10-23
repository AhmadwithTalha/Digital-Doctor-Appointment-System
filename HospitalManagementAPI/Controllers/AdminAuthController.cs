using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalManagementAPI.Helpers; // PasswordHelper
using Microsoft.Extensions.Configuration;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AdminAuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class AdminLoginDTO
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] AdminLoginDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password required." });

            var adminEmail = _configuration["Admin:Email"];
            var adminPasswordHash = _configuration["Admin:PasswordHash"];

            if (adminEmail == null || adminPasswordHash == null)
                return StatusCode(500, new { message = "Admin credentials not configured." });

            // Check email
            if (!string.Equals(dto.Email.Trim(), adminEmail.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = "Invalid admin credentials." });

            // Verify password hash
            if (!PasswordHelper.Verify(dto.Password, adminPasswordHash))
                return Unauthorized(new { message = "Invalid admin credentials." });

            // Create JWT for admin (role = Admin)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Email, adminEmail)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful.",
                token = jwt,
                expires = token.ValidTo
            });
        }
    }
}

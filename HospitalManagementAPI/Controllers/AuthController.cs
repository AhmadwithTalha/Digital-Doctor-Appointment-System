using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalManagementAPI.Data;
using HospitalManagementAPI.Models;
using HospitalManagementAPI.Helpers;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Unified login endpoint
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var email = dto.Email.Trim().ToLower();

            //Hardcoded Admin check (from appsettings.json)
            var adminEmail = _configuration["Admin:Email"]?.Trim().ToLower();
            var adminPasswordHash = _configuration["Admin:PasswordHash"];

            if (email == adminEmail && PasswordHelper.Verify(dto.Password, adminPasswordHash))
            {

                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Email, adminEmail),
            new Claim("UserType", "0")
        };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(4),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var handler = new JwtSecurityTokenHandler();
                var token = handler.CreateToken(tokenDescriptor);
                var jwt = handler.WriteToken(token);

                return Ok(new
                {
                    message = "Admin login successful",
                    token = jwt,
                    role = "Admin",
                    userType = 0
                });
            }

            //Otherwise, check in Users table
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            if (!user.IsActive || user.Status == "Blocked")
                return Forbid();

            if (!PasswordHelper.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            string responsibilitiesClaim = string.Empty;
            if (user.UserType == 1)
            {
                var sub = await _context.SubAdmins.FirstOrDefaultAsync(s => s.SubAdminId == user.RefId);
                if (sub != null && sub.Responsibilities.Any())
                {
                    responsibilitiesClaim = string.Join(',', sub.Responsibilities);
                }
            }
            //Create JWT for Patient/Doctor/SubAdmin
            var key2 = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var claims2 = new List<Claim>
    {
        new Claim("UserId", user.UserId.ToString()),
        new Claim("RefId", user.RefId.ToString()),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserType", user.UserType.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("patientId", user.UserType == 3 ? user.RefId.ToString() : string.Empty),
        new Claim("doctorId", user.UserType == 2 ? user.RefId.ToString() : string.Empty),
        new Claim("Responsibilities", responsibilitiesClaim)
    };

            var tokenDescriptor2 = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims2),
                Expires = DateTime.UtcNow.AddHours(4),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key2), SecurityAlgorithms.HmacSha256Signature)
            };

            var handler2 = new JwtSecurityTokenHandler();
            var token2 = handler2.CreateToken(tokenDescriptor2);
            var jwt2 = handler2.WriteToken(token2);

            return Ok(new
            {
                message = "Login successful",
                token = jwt2,
                role = user.Role,
                userType = user.UserType,
                refId = user.RefId
            });
        }


        // DTO
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}

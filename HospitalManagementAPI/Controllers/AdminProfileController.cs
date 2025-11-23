using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagementAPI.Data;
using HospitalManagementAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AdminProfileController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        //View Admin Profile
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var systemEmail = _config["Admin:Email"]?.Trim().ToLower();

            var profile = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.SystemEmail.ToLower() == systemEmail);
            if (profile == null)
                return NotFound(new { message = "Admin profile not found." });

            return Ok(profile);
        }

        //Create or Update Admin Profile (single record)
        [HttpPost("SaveProfile")]
        public async Task<IActionResult> SaveProfile([FromBody] AdminProfile dto)
        {
            var systemEmail = _config["Admin:Email"]?.Trim().ToLower();

            var existing = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.SystemEmail.ToLower() == systemEmail);

            if (existing == null)
            {
                var profile = new AdminProfile
                {
                    FullName = dto.FullName,
                    SystemEmail = systemEmail,
                    PersonalEmail = dto.PersonalEmail,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AdminProfiles.Add(profile);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin profile created successfully." });
            }
            else
            {
                existing.FullName = dto.FullName;
                existing.PersonalEmail = dto.PersonalEmail;
                existing.PhoneNumber = dto.PhoneNumber;
                existing.Address = dto.Address;
                existing.ImageUrl = dto.ImageUrl;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Admin profile updated successfully." });
            }
        }
    }
}

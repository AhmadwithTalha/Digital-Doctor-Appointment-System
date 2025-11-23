using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Models;
using HospitalManagementAPI.Helpers;
using System.Linq;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SubAdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubAdminController(AppDbContext context) => _context = context;

        // Create SubAdmin (Admin only)
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateSubAdminDTO dto)
        {
            if (await _context.SubAdmins.AnyAsync(s => s.Email.ToLower() == dto.Email.ToLower()))
                return BadRequest(new { message = "SubAdmin with this email already exists." });

            var sub = new SubAdmin
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Responsibilities = dto.Responsibilities ?? new List<string>(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.SubAdmins.AddAsync(sub);
            await _context.SaveChangesAsync();

            // Create User record (unified Users table) for login
            var user = new User
            {
                RefId = sub.SubAdminId,
                Email = dto.Email,
                PasswordHash = PasswordHelper.Hash(dto.Password),
                Role = "SubAdmin",
                UserType = 1,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "SubAdmin created.", subAdminId = sub.SubAdminId });
        }

        // Get all subadmins
        [HttpGet("All")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.SubAdmins
                .Select(s => new SubAdminResponseDTO
                {
                    SubAdminId = s.SubAdminId,
                    FullName = s.FullName,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    Address = s.Address,
                    Responsibilities = s.Responsibilities,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // Get by id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var s = await _context.SubAdmins.FindAsync(id);
            if (s == null) return NotFound();
            return Ok(new SubAdminResponseDTO
            {
                SubAdminId = s.SubAdminId,
                FullName = s.FullName,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                Responsibilities = s.Responsibilities,
                CreatedAt = s.CreatedAt
            });
        }

        // Update subadmin (admin only)
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSubAdminDTO dto)
        {
            var s = await _context.SubAdmins.FindAsync(id);
            if (s == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FullName)) s.FullName = dto.FullName;
            if (dto.PhoneNumber != null) s.PhoneNumber = dto.PhoneNumber;
            if (dto.Address != null) s.Address = dto.Address;
            if (dto.Responsibilities != null) s.Responsibilities = dto.Responsibilities;
            s.UpdatedAt = DateTime.UtcNow;

            _context.SubAdmins.Update(s);
            await _context.SaveChangesAsync();

            return Ok(new { message = "SubAdmin updated." });
        }

        // Delete subadmin (admin only) — this will remove the SubAdmin record and (optionally) related User record.
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await _context.SubAdmins.FindAsync(id);
            if (s == null) return NotFound();

            // remove subadmin
            _context.SubAdmins.Remove(s);

            // remove corresponding user(s)
            var users = await _context.Users.Where(u => u.UserType == 1 && u.RefId == s.SubAdminId).ToListAsync();
            if (users.Any())
                _context.Users.RemoveRange(users);

            await _context.SaveChangesAsync();

            return Ok(new { message = "SubAdmin deleted." });
        }

        // Replace responsibilities (whole list) (Admin only)
        [HttpPut("Responsibilities/{id}")]
        public async Task<IActionResult> SetResponsibilities(int id, [FromBody] List<string> responsibilities)
        {
            var s = await _context.SubAdmins.FindAsync(id);
            if (s == null) return NotFound();

            s.Responsibilities = responsibilities ?? new List<string>();
            s.UpdatedAt = DateTime.UtcNow;

            _context.SubAdmins.Update(s);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Responsibilities updated." });
        }
    }
}

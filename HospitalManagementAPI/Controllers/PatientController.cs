using Azure.Core;
using BCrypt.Net;
using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Helpers;
using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Numerics;
using System.Security.Claims;
using System.Text;



namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PatientController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("Patient-Register")]
        public async Task<IActionResult> Register([FromBody] PatientRegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                return BadRequest(new { message = "Email already registered." });

            // Create User first
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = PasswordHelper.Hash(dto.Password),
                Role = "Patient",
                UserType = 3,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt= DateTime.UtcNow

            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Patient and link to user
            var patient = new Patient
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Gender = dto.Gender,
                Age = dto.Age,
                Phone = dto.Phone,
                Address = dto.Address,
                UserId = user.UserId
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Update RefId in User table
            user.RefId = patient.PatientId;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Patient registered successfully." });
        }

        // GET: api/Patient/MyAppointments
        [Authorize(Roles = "Patient")]
        [HttpGet("Patient-MyAppointments")]
        public async Task<IActionResult> MyAppointments()
        {
            // Patient endpoints should require auth — can be public for now or add [Authorize(Roles="Patient")]
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            int patientId = 0;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                // extract patientId from token if provided
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var jwt = handler.ReadJwtToken(token);
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;
                    int.TryParse(claim, out patientId);
                }
                catch { patientId = 0; }
            }
            if (patientId == 0) return Unauthorized(new { message = "Authorization required (include Bearer token)." });

            var list = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var result = list.Select(a => new AppointmentDTO
            {
                AppointmentId = a.AppointmentId,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor?.Name ?? string.Empty,
                PatientId = a.PatientId,
                PatientName = a.Patient?.FullName ?? string.Empty,
                TokenNumber = a.TokenNumber,
                Date = a.Date,
                Status = a.Status,
                ScheduleId = a.ScheduleId
            });

            return Ok(result);
        }

        // PUT: api/Appointment/Cancel/{id}
        [HttpPut("CancelAppointment/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = "Appointment not found." });

            if (appt.Status == "Cancelled")
                return BadRequest(new { message = "Appointment already cancelled." });

            appt.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled." });
        }

        [Authorize(Roles = "Patient")]
        [HttpGet("Patient-HistoryOfAppointments")]
        public async Task<IActionResult> GetPatientHistory(DateTime? from = null, DateTime? to = null)
        {
            var patientIdClaim = User.FindFirst("patientId")?.Value;
            if (string.IsNullOrEmpty(patientIdClaim))
                return Unauthorized(new { message = "Invalid or missing token." });

            int patientId = int.Parse(patientIdClaim);

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Doctor.Department)
                .Where(a => a.PatientId == patientId);

            if (from.HasValue)
                query = query.Where(a => a.Date >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(a => a.Date <= to.Value.Date);

            var history = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    a.AppointmentId,
                    DoctorName = a.Doctor.Name,
                    Department = a.Doctor.Department.DepartmentName,
                    a.Date,
                    a.Status,
                    a.TokenNumber
                })
                .ToListAsync();

            if (!history.Any())
                return NotFound(new { message = "No appointment history found." });

            return Ok(history);
        }

        [Authorize(Roles = "Patient")]
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "Missing or invalid authorization token." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var patientIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;

            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized(new { message = "Invalid patient token." });

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null || patient.User == null)
                return NotFound(new { message = "Patient or user not found." });

            var user = patient.User;

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Old password is incorrect." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "New password must be at least 6 characters long." });

            if (dto.NewPassword == dto.OldPassword)
                return BadRequest(new { message = "New password must be different from old password." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }



        //Update Profile (Patient Only)
        [Authorize(Roles = "Patient")]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDTO dto)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "Missing or invalid authorization token." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var patientIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;

            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized(new { message = "Invalid patient token." });

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            patient.FullName = dto.FullName ?? patient.FullName;
            patient.Gender = dto.Gender ?? patient.Gender;
            patient.Age = dto.Age ?? patient.Age;
            patient.Phone = dto.Phone ?? patient.Phone;
            patient.Address = dto.Address ?? patient.Address;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                patient.FullName,
                patient.Email,
                patient.Gender,
                patient.Age,
                patient.Phone,
                patient.Address
            });
        }

        [Authorize(Roles = "Patient")]
        [HttpDelete("SelfDelete")]
        public async Task<IActionResult> SelfDelete()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token. UserId not found." });

            int userId = int.Parse(userIdClaim);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound(new { message = "User account not found." });

            if (user.UserType != 3)
                return BadRequest(new { message = "Only patients can delete their account." });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == user.RefId);

            if (patient == null)
                return NotFound(new { message = "Patient record not found." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete Patient
                _context.Patients.Remove(patient);

                // Delete User
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Your account has been deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error deleting account.", error = ex.Message });
            }
        }

    }
}

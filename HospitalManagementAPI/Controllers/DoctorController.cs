using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Helpers;
using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace HospitalManagementAPI.Controllers
{

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }


    [Route("api/[controller]")]
    [ApiController]
    public class DoctorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public DoctorController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //Get logged-in doctor ID from JWT
        private int GetDoctorId()
        {
            var doctorIdClaim = User.FindFirst("DoctorId");
            if (doctorIdClaim == null)
                throw new Exception("DoctorId not found in token.");
            return int.Parse(doctorIdClaim.Value);
        }

        //Get Tomorrow's Appointments
        [HttpGet("GetListOfTomorrowAppointments")]
        public IActionResult GetTomorrowAppointments()
        {
            int doctorId = GetDoctorId();
            var tomorrow = DateTime.Today.AddDays(1);

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.Date.Date == tomorrow.Date)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.TokenNumber,
                    PatientName = a.Patient.FullName,
                    a.Status,
                    a.Date
                })
                .OrderBy(a => a.TokenNumber)
                .ToList();

            return Ok(appointments);
        }

        //Get Appointment History (by date)
        [HttpGet("GetHistoryOfAppointmentsByDate")]
        public IActionResult GetHistoryByDate([FromQuery] DateTime date)
        {
            int doctorId = GetDoctorId();

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.Date.Date == date.Date)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.TokenNumber,
                    PatientName = a.Patient != null ? a.Patient.FullName : "Unknown",
                    a.Status,
                    a.Date
                })
                .OrderBy(a => a.TokenNumber)
                .ToList();

            if (!appointments.Any())
                return NotFound($"No appointments found on {date:yyyy-MM-dd}.");

            var total = appointments.Count;
            var done = appointments.Count(a => a.Status == "Done");
            var cancelled = appointments.Count(a => a.Status == "Cancelled");
            var booked = appointments.Count(a => a.Status == "Booked");

            var doctorFee = _context.Doctors
                .Where(d => d.DoctorId == doctorId)
                .Select(d => d.Fee)
                .FirstOrDefault();

            var totalFee = done * doctorFee;

            return Ok(new
            {
                Date = date.ToString("yyyy-MM-dd"),
                TotalAppointments = total,
                Completed = done,
                Cancelled = cancelled,
                Booked = booked,
                TotalFeeCollected = totalFee,
                Details = appointments
            });
        }

        [HttpGet("MyProfile")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMyProfile()
        {
            var claim = User.FindFirst("doctorId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return Unauthorized();

            if (!int.TryParse(claim, out int doctorId))
                return Unauthorized();

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null)
                return NotFound("Doctor not found.");

            var dto = new DoctorProfileDTO
            {
                DoctorId = doctor.DoctorId,
                Name = doctor.Name,
                Email = doctor.Email,
                Specialization = doctor.Specialization,
                DepartmentId = doctor.DepartmentId,
                DepartmentName = doctor.Department?.DepartmentName ?? string.Empty,
                RoomNo = doctor.RoomNo,
                Fee = doctor.Fee
            };

            return Ok(dto);
        }
        // PUT: api/Doctor/UpdateProfile
        // Doctor can update email and fee only
       
        [HttpPut("UpdateProfile")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateProfile([FromBody] DoctorUpdateDTO dto)
        {
            var claim = User.FindFirst("doctorId")?.Value;
            if (string.IsNullOrEmpty(claim))
                return Unauthorized();
            if (!int.TryParse(claim, out int doctorId))
                return Unauthorized();

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
                return NotFound("Doctor not found.");

            // if changing email ensure uniqueness
            if (!string.Equals(dto.Email?.Trim(), doctor.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _context.Doctors
                    .AnyAsync(d => d.Email == dto.Email && d.DoctorId != doctorId);

                if (emailExists)
                    return BadRequest(new { message = "Email is already in use by another account." });

                doctor.Email = dto.Email.Trim();
            }

            // validate fee
            if (dto.Fee <= 0)
                return BadRequest(new { message = "Fee must be greater than zero." });

            doctor.Fee = dto.Fee;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }

        // PUT: api/Doctor/ChangePassword
        [HttpPut("ChangePassword")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var doctorIdClaim = User.FindFirst("doctorId")?.Value;
            if (string.IsNullOrEmpty(doctorIdClaim))
                return Unauthorized(new { message = "Doctor ID not found in token." });

            if (!int.TryParse(doctorIdClaim, out int doctorId))
                return Unauthorized(new { message = "Invalid doctor ID." });

            var doctor = await _context.Doctors
                .Include(d => d.User) // include User to access password
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                return NotFound(new { message = "Doctor not found." });

            var user = doctor.User;
            if (user == null)
                return NotFound(new { message = "User account not found for this doctor." });

            // ✅ Verify old password (using BCrypt or your helper)
            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Old password is incorrect." });

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "New password must be at least 6 characters long." });

            if (dto.NewPassword == dto.OldPassword)
                return BadRequest(new { message = "New password must be different from old password." });

            // ✅ Hash and update the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }


        // POST: api/Doctor/RequestPasswordReset
        // (Public) - sends email with a reset token link via SendGrid
        [HttpPost("RequestPasswordReset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] HospitalManagementAPI.DTOs.PasswordResetRequestDTO requestDto)
        {
            if (string.IsNullOrEmpty(requestDto.Email))
                return BadRequest(new { message = "Email is required." });

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == requestDto.Email);
            if (doctor == null)
                return NotFound(new { message = "Email not found." });

            // create token
            var token = Guid.NewGuid().ToString("N");
            var reset = new PasswordResetRequest
            {
                DoctorId = doctor.DoctorId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            _context.PasswordResetRequests.Add(reset);
            await _context.SaveChangesAsync();

            // prepare reset link (frontend url from config)
            var frontendBase = _configuration["Frontend:BaseUrl"] ?? "https://your-frontend-url";
            var resetUrl = $"{frontendBase.TrimEnd('/')}/reset-password?token={token}";

            // send email via SendGrid if configured
            var sendGridKey = _configuration["SendGrid:ApiKey"];
            if (!string.IsNullOrEmpty(sendGridKey))
            {
                try
                {
                    var client = new SendGrid.SendGridClient(sendGridKey);
                    var fromEmail = _configuration["SendGrid:FromEmail"] ?? "no-reply@hospital.local";
                    var fromName = _configuration["SendGrid:FromName"] ?? "Hospital System";

                    var msg = new SendGrid.Helpers.Mail.SendGridMessage()
                    {
                        From = new SendGrid.Helpers.Mail.EmailAddress(fromEmail, fromName),
                        Subject = "Password reset request",
                        PlainTextContent = $"You (or someone else) requested a password reset. Use this link to reset your password: {resetUrl} (expires in 1 hour).",
                        HtmlContent = $"<p>You requested a password reset. Click the link to reset your password (expires in 1 hour):</p><p><a href='{resetUrl}'>Reset password</a></p>"
                    };
                    msg.AddTo(doctor.Email);
                    await client.SendEmailAsync(msg);
                }
                catch
                {
                    // swallow send errors but keep token in DB so you can test by reading DB manually
                }
            }

            // respond (do not reveal too much info)
            return Ok(new { message = "If the email exists, a password reset link has been issued." });
        }

        // POST: api/Doctor/ResetPassword
        // (Public) — reset using token
        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.NewPassword))
                return BadRequest(new { message = "Token and new password are required." });

            // include related Doctor + User (because password is in User table)
            var reset = await _context.PasswordResetRequests
                .Include(r => r.Doctor)
                    .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(r => r.Token == dto.Token);

            if (reset == null)
                return BadRequest(new { message = "Invalid or expired token." });

            if (reset.IsUsed || reset.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "Token is invalid or expired." });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "New password must be at least 6 characters." });

            //Access related User (doctor's account)
            var user = reset.Doctor?.User;
            if (user == null)
                return BadRequest(new { message = "Associated user account not found." });

            //Hash and update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            //Mark token as used
            reset.IsUsed = true;
            reset.ExpiresAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }

    }

}

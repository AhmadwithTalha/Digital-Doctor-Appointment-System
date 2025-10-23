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

        // POST: api/Patient/Register
        [HttpPost("Patient-Register")]
        public async Task<IActionResult> Register([FromBody] PatientRegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            var exists = await _context.Patients.AnyAsync(p => p.Email == dto.Email);
            if (exists)
                return BadRequest(new { message = "Email already registered." });
            string hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var patient = new Patient
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Password = PasswordHelper.Hash(dto.Password), // TODO: hash in production
                Gender = dto.Gender,
                Age = dto.Age,
                Phone = dto.Phone,
                Address = dto.Address
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Patient registered successfully." });
        }

        // POST: api/Patient/Login
        [HttpPost("Patient-Login")]
        public async Task<IActionResult> Login([FromBody] PatientLoginDTO dto)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == dto.Email);
            if (!PasswordHelper.Verify(dto.Password, patient.Password))
                return BadRequest(new { message = "Invalid email or password." });


            // generate JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("patientId", patient.PatientId.ToString()),
                    new Claim(ClaimTypes.Email, patient.Email),
                    new Claim(ClaimTypes.Role, patient.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                message = "Login successful!",
                token = tokenHandler.WriteToken(token),
                patientId = patient.PatientId,
                name = patient.FullName,
                role = patient.Role
            });
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
            // 🧩 Get patient ID from JWT token
            var patientIdClaim = User.FindFirst("patientId")?.Value;
            if (string.IsNullOrEmpty(patientIdClaim))
                return Unauthorized(new { message = "Invalid or missing token." });

            int patientId = int.Parse(patientIdClaim);

            // 🧠 Base query
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Doctor.Department)
                .Where(a => a.PatientId == patientId);

            // 📅 Optional date filters
            if (from.HasValue)
                query = query.Where(a => a.Date >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(a => a.Date <= to.Value.Date);

            // 🗂 Fetch and format data
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
            // Extract patient ID from token
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "Missing or invalid authorization token." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var patientIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;

            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized(new { message = "Invalid patient token." });

            // Find patient
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            // Verify old password
            if (!PasswordHelper.Verify(dto.OldPassword, patient.Password))
                return BadRequest(new { message = "Old password is incorrect." });

            // Update new password (hash)
            patient.Password = PasswordHelper.Hash(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }


        // ✅ Update Profile (Patient Only)
        [Authorize(Roles = "Patient")]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePatientProfileDTO dto)
        {
            // Extract patient ID from token
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { message = "Missing or invalid authorization token." });

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var patientIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;

            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized(new { message = "Invalid patient token." });

            // Find patient
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            // Update allowed fields only
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
    }
}

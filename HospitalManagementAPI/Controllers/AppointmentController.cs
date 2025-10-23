
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Models;
using System.Security.Claims;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentController(AppDbContext context)
        {
            _context = context;
        }

       
        // Helper: extract claim values
        
        private int GetPatientIdFromToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type.Equals("patientId", StringComparison.OrdinalIgnoreCase));
            if (claim == null || !int.TryParse(claim.Value, out var id)) return 0;
            return id;
        }

        private int GetDoctorIdFromToken()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type.Equals("doctorId", StringComparison.OrdinalIgnoreCase)
                                                     || c.Type.Equals("DoctorId", StringComparison.OrdinalIgnoreCase));
            if (claim == null || !int.TryParse(claim.Value, out var id)) return 0;
            return id;
        }

     
        // POST: api/Appointment/Book
        // Patient must be authenticated (token must include patientId claim)
        
        [HttpPost("Book")]
        public async Task<IActionResult> Book([FromBody] BookAppointmentDTO dto)
        {
            //Get PatientId from JWT Token
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            int patientId = 0;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                try
                {
                    var jwt = handler.ReadJwtToken(token);
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;
                    int.TryParse(claim, out patientId);
                }
                catch { patientId = 0; }
            }
            if (patientId == 0) return Unauthorized(new { message = "Authorization required (include Bearer token)." });

            // Validate Doctor
            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.DoctorId == dto.DoctorId);
            if (doctor == null)
                return BadRequest(new { message = "Doctor not found." });

            // Validate Schedule (if provided)
            DoctorSchedule? schedule = null;
            if (dto.ScheduleId.HasValue)
            {
                schedule = await _context.DoctorSchedules.FindAsync(dto.ScheduleId.Value);
                if (schedule == null)
                    return BadRequest(new { message = "Schedule not found." });
            }

            var appointmentDate = dto.Date.Date;

            // Check duplicate appointment for same patient, doctor, and date
            var existing = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.DoctorId == dto.DoctorId &&
                    a.PatientId == patientId &&
                    a.Date == appointmentDate &&
                    a.Status != "Cancelled");

            if (existing != null)
                return BadRequest(new { message = "You already have an appointment with this doctor on this date." });

            // Check max patients limit
            var count = await _context.Appointments
                .CountAsync(a => a.DoctorId == dto.DoctorId && a.Date == appointmentDate && a.Status != "Cancelled");
            if (schedule != null && count >= schedule.MaxPatients)
                return BadRequest(new { message = "This doctor's schedule is fully booked for the selected date." });

            // Assign next token number
            int tokenNumber = count + 1;

            var appointment = new Appointment
            {
                DoctorId = dto.DoctorId,
                PatientId = patientId,
                ScheduleId = dto.ScheduleId,
                TokenNumber = tokenNumber,
                Date = appointmentDate,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var patient = await _context.Patients.FindAsync(patientId);

            // Build response object
            return Ok(new
            {
                message = "Appointment booked successfully.",
                appointmentId = appointment.AppointmentId,
                patientId = patient?.PatientId,
                patientName = patient?.FullName,
                tokenNumber = appointment.TokenNumber,
                doctorName = doctor.Name,
                departmentName = doctor.Department?.DepartmentName,
                roomNo = doctor.RoomNo,
                scheduleTime = schedule != null
                    ? $"{DateTime.Today.Add(schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(schedule.EndTime):hh:mm tt}"
                    : "N/A",
                date = appointment.Date.ToString("yyyy-MM-dd")
            });
        }


     
        // GET: api/Appointment/DoctorToday/{doctorId}?date=yyyy-MM-dd
        // Doctor (or Admin) can view appointments for a date (default = today)
      
        [HttpGet("DoctorCheackTodayAppointments/{doctorId}")]
        [Authorize] // require JWT; we will ensure role/ownership below
        public async Task<IActionResult> DoctorToday(int doctorId, [FromQuery] DateTime? date)
        {
            var requestDate = date?.Date ?? DateTime.UtcNow.Date;

            // If caller is a doctor, ensure they only access their own data
            var callerDoctorId = GetDoctorIdFromToken();
            var isDoctor = callerDoctorId > 0;
            var isAdmin = User.IsInRole("Admin");

            if (isDoctor && !isAdmin && callerDoctorId != doctorId)
                return Forbid();

            var list = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.Date == requestDate && a.Status != "Cancelled")
                .OrderBy(a => a.TokenNumber)
                .ToListAsync();

            var result = list.Select(a => new
            {
                a.AppointmentId,
                a.TokenNumber,
                a.PatientId,
                PatientName = a.Patient?.FullName,
                a.Status,
                Date = a.Date.ToString("yyyy-MM-dd")
            });

            return Ok(new
            {
                doctorId,
                date = requestDate.ToString("yyyy-MM-dd"),
                appointments = result
            });
        }

     
        // PUT: api/Appointment/Cancel/{id}
        // Patient cancels their own appointment (or Admin)
       
        [HttpPut("CancelPatientownAppointment/{id}")]
        [Authorize] // patient or admin
        public async Task<IActionResult> Cancel(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = "Appointment not found." });

            // If already cancelled
            if (appt.Status == "Cancelled")
                return BadRequest(new { message = "Appointment already cancelled." });

            // Authorization: either patient who owns it or Admin
            var patientId = GetPatientIdFromToken();
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && patientId != appt.PatientId)
                return Forbid();

            // Allow cancellation only if not Done
            if (appt.Status == "Done")
                return BadRequest(new { message = "Cannot cancel an appointment that is already marked Done." });

            appt.Status = "Cancelled";
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled." });
        }

        
        // PUT: api/Appointment/MarkDone/{id}
        // Doctor marks an appointment as Done
        
        [HttpPut("DoctorMarkDoneAppointment/{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> MarkDone(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = "Appointment not found." });

            // Authorization: doctor who owns appointment or admin
            var doctorId = GetDoctorIdFromToken();
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && doctorId != appt.DoctorId)
                return Forbid();

            if (appt.Status == "Done")
                return BadRequest(new { message = "Appointment already marked as Done." });

            appt.Status = "Done";
            appt.UpdatedAt = DateTime.UtcNow;

          

            await _context.SaveChangesAsync();

            // Find next pending appointment (same doctor & date)
            var next = await _context.Appointments
                .Where(a => a.DoctorId == appt.DoctorId && a.Date == appt.Date && a.Status == "Pending" && a.TokenNumber > appt.TokenNumber)
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Appointment marked as Done.",
                appointmentId = appt.AppointmentId,
                nextAppointment = next == null ? null : new
                {
                    next.AppointmentId,
                    next.TokenNumber,
                    next.PatientId
                }
            });
        }

        // PUT: api/Appointment/MarkNoShow/{id}
        // Doctor marks an appointment as NoShow (skip)
      
        [HttpPut("DoctorMarkNoShowAppointment/{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound(new { message = "Appointment not found." });

            var doctorId = GetDoctorIdFromToken();
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && doctorId != appt.DoctorId)
                return Forbid();

            if (appt.Status == "NoShow")
                return BadRequest(new { message = "Appointment already marked as NoShow." });

            appt.Status = "NoShow";
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Find next pending appointment (same doctor & date)
            var next = await _context.Appointments
                .Where(a => a.DoctorId == appt.DoctorId && a.Date == appt.Date && a.Status == "Pending" && a.TokenNumber > appt.TokenNumber)
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Appointment marked as NoShow.",
                appointmentId = appt.AppointmentId,
                nextAppointment = next == null ? null : new
                {
                    next.AppointmentId,
                    next.TokenNumber,
                    next.PatientId
                }
            });
        }

     
        // GET: api/Appointment/MyAppointments
        // Patient can view his/her appointments (requires patient token)
        [HttpGet("PatientMyAppointments")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> MyAppointments()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            int patientId = 0;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                try
                {
                    var jwt = handler.ReadJwtToken(token);
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "patientId")?.Value;
                    int.TryParse(claim, out patientId);
                }
                catch { patientId = 0; }
            }
            if (patientId == 0)
                return Unauthorized(new { message = "Authorization required (include Bearer token)." });

            var list = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department)
                .Include(a => a.Schedule)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var result = list.Select(a => new
            {
                appointmentId = a.AppointmentId,
                patientId = a.PatientId,
                patientName = a.Patient?.FullName,
                tokenNumber = a.TokenNumber,
                doctorName = a.Doctor?.Name,
                departmentName = a.Doctor?.Department?.DepartmentName,
                roomNo = a.Doctor?.RoomNo,
                scheduleTime = a.Schedule != null
                    ? $"{DateTime.Today.Add(a.Schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(a.Schedule.EndTime):hh:mm tt}"
                    : "N/A",
                date = a.Date.ToString("yyyy-MM-dd"),
                status = a.Status
            });

            return Ok(result);
        }

    }
}

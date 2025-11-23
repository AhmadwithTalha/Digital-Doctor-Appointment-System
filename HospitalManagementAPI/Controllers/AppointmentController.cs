
using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Hubs;
using HospitalManagementAPI.Models;
using HospitalManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly NotificationService _notificationService;


        public AppointmentController(AppDbContext context, IHubContext<NotificationHub> hub,NotificationService notificationService)
        {
            _context = context;
            _hub = hub;
            _notificationService = notificationService;
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
        private async Task BroadcastQueueStatusToPatients(int doctorId, DateTime date, Appointment? current)
        {
            // Get all patients who have appointments with this doctor on this date
            var patientAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId && a.Date == date && a.Status != "Cancelled")
                .ToListAsync();

            if (current == null) return;

            foreach (var appt in patientAppointments)
            {
                var notif = new
                {
                    appointmentId = appt.AppointmentId,
                    tokenNumber = appt.TokenNumber,
                    doctorName = appt.Doctor?.Name,
                    roomNo = appt.Doctor?.RoomNo,
                    currentToken = current.TokenNumber,
                    date = appt.Date.ToString("yyyy-MM-dd")
                };

                await _hub.Clients.Group($"patient-{appt.PatientId}").SendAsync("QueueStatusUpdate", notif);
            }
        }

        // private helper: broadcasts queue summary to doctor group
        private async Task BroadcastQueueStatus(int doctorId, DateTime date)
        {
            var total = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId && a.Date == date);
            var served = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId && a.Date == date && a.Status == "Done");
            var remaining = await _context.Appointments.CountAsync(a => a.DoctorId == doctorId && a.Date == date && a.Status == "Pending");
            var current = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == date && a.Status == "Pending")
                .OrderBy(a => a.TokenNumber)
                .Select(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            var status = new QueueStatusNotification(doctorId, total, served, remaining, current);
            await _hub.Clients.Group($"doctor-{doctorId}").SendAsync("QueueStatus", status);
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
                //patientName = patient != null ? x.Patient.FullName : "Unknown",
                patientName = patient?.FullName ?? "Unknown",
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
        [Authorize] 
        public async Task<IActionResult> DoctorToday(int doctorId, [FromQuery] DateTime? date)
        {
            var requestDate = date?.Date ?? DateTime.UtcNow.Date;

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
        


        // POST: api/Appointment/StartQueue/{doctorId}
        [Authorize(Roles = "Doctor")]
        [HttpPost("StartQueue/{doctorId}")]
        public async Task<IActionResult> StartQueue(int doctorId)
        {
            var today = DateTime.UtcNow.Date;
            // find the first pending appointment (lowest token) for today
            var first = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a => a.DoctorId == doctorId && a.Date == today && a.Status == "Pending")
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            if (first == null)
                return NotFound(new { message = "No pending appointments for today." });

            var notif = new NextPatientNotification(
                first.AppointmentId,
                first.PatientId,
                first.Patient?.FullName ?? string.Empty,
                first.TokenNumber,
                first.Doctor?.Name ?? string.Empty,
                first.Doctor?.Department?.DepartmentName ?? string.Empty,
                first.Doctor?.RoomNo ?? 0,
                first.Schedule != null ? $"{DateTime.Today.Add(first.Schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(first.Schedule.EndTime):hh:mm tt}" : "N/A",
                first.Date.ToString("yyyy-MM-dd")
            );

            await _hub.Clients.Group($"patient-{first.PatientId}").SendAsync("NextPatient", notif);
            await _hub.Clients.Group($"doctor-{doctorId}").SendAsync("NowServing", notif);
            await BroadcastQueueStatusToPatients(doctorId, today, first);
            await BroadcastQueueStatus(doctorId, today);


            await _notificationService.SendNotificationAsync($"patient-{first.PatientId}", $"Your turn! Token #{first.TokenNumber} with Dr. {first.Doctor?.Name}");
            await _notificationService.SendNotificationAsync($"doctor-{doctorId}", $"Now serving Token #{first.TokenNumber} ({first.Patient?.FullName})");
            return Ok(new { message = "Queue started. First patient notified.", appointmentId = first.AppointmentId });
        }

        // PUT: api/Appointment/MarkDone/{id}
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPut("MarkDone/{id}")]
        public async Task<IActionResult> MMarkDoneWithNotification(int id)
        {
            var appt = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appt == null) return NotFound(new { message = "Appointment not found." });

            appt.Status = "Done";
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationAsync($"patient-{appt.PatientId}", $"Your appointment with Dr. {appt.Doctor?.Name} is marked Done.");
            await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", $"Token #{appt.TokenNumber} ({appt.Patient?.FullName}) marked Done.");

            var next = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.DoctorId == appt.DoctorId && a.Date == appt.Date && a.Status == "Pending")
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                var notifNext = new NextPatientNotification(
                    next.AppointmentId,
                    next.PatientId,
                    next.Patient?.FullName ?? string.Empty,
                    next.TokenNumber,
                    appt.Doctor?.Name ?? string.Empty,
                    appt.Doctor?.Department?.DepartmentName ?? string.Empty,
                    appt.Doctor?.RoomNo ?? 0,
                    next.Schedule != null ? $"{DateTime.Today.Add(next.Schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(next.Schedule.EndTime):hh:mm tt}" : "N/A",
                    next.Date.ToString("yyyy-MM-dd")
                );

                await _hub.Clients.Group($"patient-{next.PatientId}").SendAsync("NextPatient", notifNext);
                await _hub.Clients.Group($"doctor-{appt.DoctorId}").SendAsync("NowServing", notifNext);

                // Save notification for next patient
                await _notificationService.SendNotificationAsync($"patient-{next.PatientId}", $"Your turn! Token #{next.TokenNumber} with Dr. {appt.Doctor?.Name}");
                await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", $"Now serving Token #{next.TokenNumber} ({next.Patient?.FullName})");
            }
            else
            {
                await _hub.Clients.Group($"doctor-{appt.DoctorId}").SendAsync("QueueEmpty", new SimpleNotification("Queue", "No more pending patients for today."));
                await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", "Queue is empty for today.");
            }

            await BroadcastQueueStatus(appt.DoctorId, appt.Date);
            await BroadcastQueueStatusToPatients(appt.DoctorId, appt.Date, next);

            return Ok(new { message = "Appointment marked as Done." });
        }


        // PUT: api/Appointment/Skip/{id}
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPut("Skip/{id}")]
        public async Task<IActionResult> Skip(int id)
        {
            var appt = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appt == null) return NotFound(new { message = "Appointment not found." });

            appt.Status = "Skipped";
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Save notification for skipped appointment
            await _notificationService.SendNotificationAsync($"patient-{appt.PatientId}", $"Your appointment with Dr. {appt.Doctor?.Name} has been skipped.");
            await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", $"Token #{appt.TokenNumber} ({appt.Patient?.FullName}) has been skipped.");

            // Find next pending
            var next = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.DoctorId == appt.DoctorId && a.Date == appt.Date && a.Status == "Pending")
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                var notifNext = new NextPatientNotification(
                    next.AppointmentId,
                    next.PatientId,
                    next.Patient?.FullName ?? string.Empty,
                    next.TokenNumber,
                    appt.Doctor?.Name ?? string.Empty,
                    appt.Doctor?.Department?.DepartmentName ?? string.Empty,
                    appt.Doctor?.RoomNo ?? 0,
                    next.Schedule != null ? $"{DateTime.Today.Add(next.Schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(next.Schedule.EndTime):hh:mm tt}" : "N/A",
                    next.Date.ToString("yyyy-MM-dd")
                );

                await _hub.Clients.Group($"patient-{next.PatientId}").SendAsync("NextPatient", notifNext);
                await _hub.Clients.Group($"doctor-{appt.DoctorId}").SendAsync("NowServing", notifNext);

                // Save notification for next patient
                await _notificationService.SendNotificationAsync($"patient-{next.PatientId}", $"Your turn! Token #{next.TokenNumber} with Dr. {appt.Doctor?.Name}");
                await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", $"Now serving Token #{next.TokenNumber} ({next.Patient?.FullName})");
            }
            else
            {
                await _hub.Clients.Group($"doctor-{appt.DoctorId}").SendAsync("QueueEmpty", new SimpleNotification("Queue", "No more pending patients for today."));
                await _notificationService.SendNotificationAsync($"doctor-{appt.DoctorId}", "Queue is empty for today.");
            }

            await BroadcastQueueStatus(appt.DoctorId, appt.Date);
            await BroadcastQueueStatusToPatients(appt.DoctorId, appt.Date, next);

            return Ok(new { message = "Appointment marked as Skipped." });
        }



    }
}

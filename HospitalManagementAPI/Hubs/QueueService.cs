using HospitalManagementAPI.Data;
using HospitalManagementAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalManagementAPI.Services
{
    public class QueueService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public QueueService(AppDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task StartQueueAsync(int doctorId)
        {
            var today = DateTime.UtcNow.Date;

            var first = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a => a.DoctorId == doctorId && a.Date == today && a.Status == "Pending")
                .OrderBy(a => a.TokenNumber)
                .FirstOrDefaultAsync();

            if (first != null)
            {
                var notif = new
                {
                    first.AppointmentId,
                    first.PatientId,
                    PatientName = first.Patient?.FullName ?? "",
                    first.TokenNumber,
                    DoctorName = first.Doctor?.Name ?? "",
                    Department = first.Doctor?.Department?.DepartmentName ?? "",
                    RoomNo = first.Doctor?.RoomNo ?? 0,
                    Schedule = first.Schedule != null ? $"{DateTime.Today.Add(first.Schedule.StartTime):hh:mm tt} - {DateTime.Today.Add(first.Schedule.EndTime):hh:mm tt}" : "N/A",
                    Date = first.Date.ToString("yyyy-MM-dd")
                };

                await _hub.Clients.Group($"patient-{first.PatientId}").SendAsync("NextPatient", notif);
                await _hub.Clients.Group($"doctor-{doctorId}").SendAsync("NowServing", notif);
            }
        }

        public async Task MarkAppointmentDoneAsync(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) return;

            appointment.Status = "Done";
            await _context.SaveChangesAsync();

            await StartQueueAsync(appointment.DoctorId);
        }

        public async Task<object> GetQueueStatsAsync(int doctorId)
        {
            var today = DateTime.UtcNow.Date;

            var total = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == today)
                .CountAsync();

            var done = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == today && a.Status == "Done")
                .CountAsync();

            var pending = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == today && a.Status == "Pending")
                .CountAsync();

            var cancelled = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Date == today && a.Status == "Cancelled")
                .CountAsync();

            return new
            {
                Total = total,
                Done = done,
                Pending = pending,
                Cancelled = cancelled
            };
        }
    }
}

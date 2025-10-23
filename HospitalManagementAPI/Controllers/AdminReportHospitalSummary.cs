using HospitalManagementAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // ✅ Only admin can access this report
    public class AdminReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminReportController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/AdminReport/HospitalSummary
        [HttpGet("HospitalSummaryWithTotalFee")]
        public async Task<IActionResult> GetHospitalSummary(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? departmentId = null,
            int? doctorId = null)
        {
            // Default date range = all time
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .ThenInclude(d => d.Department)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value.Date);

            if (departmentId.HasValue)
                query = query.Where(a => a.Doctor.DepartmentId == departmentId.Value);

            if (doctorId.HasValue)
                query = query.Where(a => a.DoctorId == doctorId.Value);

            // Fetch data
            var data = await query.ToListAsync();

            // Group data by Doctor (and Department)
            var report = data
                .GroupBy(a => new
                {
                    a.DoctorId,
                    DoctorName = a.Doctor.Name,
                    DepartmentId = a.Doctor.DepartmentId,
                    DepartmentName = a.Doctor.Department.DepartmentName,
                    Fee = a.Doctor.Fee // ✅ decimal (not nullable)
                })
                .Select(g => new
                {
                    g.Key.DoctorId,
                    g.Key.DoctorName,
                    g.Key.DepartmentId,
                    g.Key.DepartmentName,
                    TotalAppointments = g.Count(),
                    DoneAppointments = g.Count(a => a.Status == "Done"),
                    CancelledAppointments = g.Count(a => a.Status == "Cancelled"),
                    PendingAppointments = g.Count(a => a.Status == "Pending"),
                    TotalFeeCollected = g.Count(a => a.Status == "Done") * g.Key.Fee
                })
                .OrderBy(r => r.DepartmentName)
                .ThenBy(r => r.DoctorName)
                .ToList();

            // ✅ Also return hospital-wide totals
            var overall = new
            {
                TotalDepartments = report.Select(r => r.DepartmentId).Distinct().Count(),
                TotalDoctors = report.Count,
                TotalAppointments = report.Sum(r => r.TotalAppointments),
                TotalDone = report.Sum(r => r.DoneAppointments),
                TotalCancelled = report.Sum(r => r.CancelledAppointments),
                TotalPending = report.Sum(r => r.PendingAppointments),
                TotalFeeCollected = report.Sum(r => r.TotalFeeCollected)
            };

            return Ok(new
            {
                message = "Hospital summary report generated successfully.",
                overall,
                report
            });
        }
    }
}

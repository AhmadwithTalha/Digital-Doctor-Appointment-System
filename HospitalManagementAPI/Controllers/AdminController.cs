using BCrypt.Net; 
using HospitalManagementAPI.Data;
using HospitalManagementAPI.Helpers;
using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HospitalManagementAPI.Helpers; // PasswordHelper
using Microsoft.Extensions.Configuration;


namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
       
       

        
        // ✅ GET: api/Admin/GetRegisteredPatients?startDate=2025-10-01&endDate=2025-10-20
        [Authorize(Roles = "Admin")]
        [HttpGet("GetRegisteredPatients")]
        public async Task<IActionResult> GetRegisteredPatients([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // Default range handling
            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // include end of today

            var patients = await _context.Patients
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.PatientId,
                    p.FullName,
                    p.Email,
                    p.Gender,
                    p.Age,
                    p.Phone,
                    p.Address,
                    RegisteredOn = p.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            if (!patients.Any())
                return NotFound(new { message = "No patients found for the selected period." });

            return Ok(new
            {
                totalPatients = patients.Count,
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd"),
                data = patients
            });
        }

            // ✅ Add new Department
            [Authorize(Roles = "Admin")]
        [HttpPost("AddDepartment")]
        public IActionResult AddDepartment([FromBody] Department department)
        {
            _context.Departments.Add(department);
            _context.SaveChanges();
            return Ok(department);
        }

        // ✅ Get all Departments
        [HttpGet("GetDepartments")]
        public IActionResult GetDepartments()
        {
            return Ok(_context.Departments.ToList());
        }

        // ✅ Update Department
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateDepartment/{id}")]
        public IActionResult UpdateDepartment(int id, [FromBody] Department updatedDepartment)
        {
            var department = _context.Departments.Find(id);
            if (department == null)
                return NotFound("Department not found.");

            department.DepartmentName = updatedDepartment.DepartmentName;
            department.Description = updatedDepartment.Description;

            _context.SaveChanges();

            return Ok(department);
        }
        // ✅ Delete Department
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteDepartment/{id}")]
        public IActionResult DeleteDepartment(int id)
        {
            var department = _context.Departments.Find(id);
            if (department == null)
                return NotFound("Department not found.");

            _context.Departments.Remove(department);
            _context.SaveChanges();

            return Ok("Department deleted successfully.");
        }


        // ✅ Add Doctor
        [Authorize(Roles = "Admin")]
        [HttpPost("AddDoctor")]
        public IActionResult AddDoctor([FromBody] DoctorDTO dto)
        {
            if (dto.Fee <= 0)
                return BadRequest("Doctor fee must be greater than 0.");

            var existingDoctor = _context.Doctors
                .FirstOrDefault(d => d.Name.ToLower() == dto.Name.ToLower()
                                  && d.DepartmentId == dto.DepartmentId);

            if (existingDoctor != null)
                return BadRequest("A doctor with this name already exists in this department.");
            string hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var doctor = new Doctor
            {
                Name = dto.Name,
                Email = dto.Email,
                Specialization = dto.Specialization,
                DepartmentId = dto.DepartmentId,
                RoomNo = dto.RoomNo,
                //Password = hashed,
                Password = PasswordHelper.Hash(dto.Password),

                Fee = dto.Fee
            }; 

            _context.Doctors.Add(doctor);
            _context.SaveChanges();

            return Ok(doctor);
        }




        // ✅ Get all Doctors
        [HttpGet("GetDoctors")]
        public IActionResult GetDoctors()
        {
            return Ok(_context.Doctors.ToList());
        }

        // ✅ Update Doctor
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateDoctor/{id}")]
        public IActionResult UpdateDoctor(int id, [FromBody] Doctor updatedDoctor)
        {
            if (updatedDoctor.Fee <= 0)
            {
                return BadRequest("Doctor fee must be greater than 0.");
            }
            var doctor = _context.Doctors.Find(id);
            if (doctor == null)
                return NotFound("Doctor not found.");

            doctor.Name = updatedDoctor.Name;
            doctor.Email = updatedDoctor.Email;
            doctor.Specialization = updatedDoctor.Specialization;
            doctor.DepartmentId = updatedDoctor.DepartmentId;
            doctor.RoomNo = updatedDoctor.RoomNo;
            doctor.Fee = updatedDoctor.Fee;

            _context.SaveChanges();

            return Ok(doctor);
        }

        // ✅ Delete Doctor
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteDoctor/{id}")]
        public IActionResult DeleteDoctor(int id)
        {
            var doctor = _context.Doctors.Find(id);
            if (doctor == null)
                return NotFound("Doctor not found.");

            _context.Doctors.Remove(doctor);
            _context.SaveChanges();

            return Ok("Doctor deleted successfully.");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("AppointmentReport")]
        public async Task<IActionResult> AppointmentReport(DateTime? from = null, DateTime? to = null,
                                                  int? departmentId = null, int? doctorId = null,
                                                  int minDoctorAppointments = 0,
                                                  int samplePatientsPerDoctor = 5)
        {
            // default range: last 30 days
            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? today.AddDays(-30);
            var end = to?.Date ?? today;

            // Normalize end to end of day if necessary (we use date only comparisons)
            // Query base: appointments within date range (inclusive)
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Doctor).ThenInclude(d => d.Department)
                .Include(a => a.Patient)
                .Where(a => a.Date.Date >= start && a.Date.Date <= end);

            if (departmentId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.Doctor.DepartmentId == departmentId.Value);

            if (doctorId.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId.Value);

            // We will group by doctor and department
            var groupedByDoctor = await appointmentsQuery
                .AsNoTracking()
                .GroupBy(a => new { a.DoctorId, a.Doctor.Name, DeptId = a.Doctor.DepartmentId, DeptName = a.Doctor.Department.DepartmentName })
                .Select(g => new
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.Name,
                    DepartmentId = g.Key.DeptId,
                    DepartmentName = g.Key.DeptName,
                    TotalAppointments = g.Count(),
                    TotalDone = g.Count(x => x.Status == "Done"),
                    TotalNoShow = g.Count(x => x.Status == "Cancelled" || x.Status == "NoShow"), // handle more no-show statuses if used
                                                                                                 // get sample patient list (top N by date descending)
                    SamplePatients = g.OrderByDescending(x => x.Date)
                                      .Select(x => new {
                                          x.AppointmentId,
                                          x.PatientId,
                                          PatientName = x.Patient.FullName,
                                          x.Status,
                                          Date = x.Date
                                      })
                                      .Take(samplePatientsPerDoctor)
                                      .ToList()
                })
                .ToListAsync();

            // optionally filter doctors by minDoctorAppointments
            if (minDoctorAppointments > 0)
                groupedByDoctor = groupedByDoctor.Where(d => d.TotalAppointments >= minDoctorAppointments).ToList();

            // build department-level aggregation
            var deptGroups = groupedByDoctor
                .GroupBy(d => new { d.DepartmentId, d.DepartmentName })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartmentId,
                    DepartmentName = g.Key.DepartmentName,
                    TotalAppointments = g.Sum(x => x.TotalAppointments),
                    TotalDone = g.Sum(x => x.TotalDone),
                    TotalNoShow = g.Sum(x => x.TotalNoShow),
                    Doctors = g.Select(x => new
                    {
                        x.DoctorId,
                        x.DoctorName,
                        x.TotalAppointments,
                        x.TotalDone,
                        x.TotalNoShow,
                        SamplePatients = x.SamplePatients
                    }).ToList()
                })
                .ToList();

            // overall totals (hospital-wide for window)
            var overall = new
            {
                From = start.ToString("yyyy-MM-dd"),
                To = end.ToString("yyyy-MM-dd"),
                TotalAppointments = groupedByDoctor.Sum(x => x.TotalAppointments),
                TotalDone = groupedByDoctor.Sum(x => x.TotalDone),
                TotalNoShow = groupedByDoctor.Sum(x => x.TotalNoShow)
            };

            return Ok(new
            {
                Overall = overall,
                Departments = deptGroups
            });
        }

        



    }
}

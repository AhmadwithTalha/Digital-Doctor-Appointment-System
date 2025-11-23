using BCrypt.Net; 
using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Helpers;
using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



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
       
       

        
        //GET: api/Admin/GetRegisteredPatients?startDate=2025-10-01&endDate=2025-10-20
        [Authorize(Roles = "Admin")]
        [HttpGet("GetRegisteredPatients")]
        public async Task<IActionResult> GetRegisteredPatients([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // Default range handling
            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

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

            //Add new Department
            [Authorize(Roles = "Admin")]
        [HttpPost("AddDepartment")]
        public async Task<IActionResult> AddDepartment([FromBody] CreateDepartmentDTO dto)
        {
            var dept = new Department
            {
                DepartmentName = dto.DepartmentName,
                Description = dto.Description
            };

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Department created successfully", dept.DepartmentId });
        }


        //Get all Departments
        [HttpGet("GetDepartments")]
        public IActionResult GetDepartments()
        {
            return Ok(_context.Departments.ToList());
        }

        //Update Department
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateDepartment/{id}")]
        public IActionResult UpdateDepartment(int id, [FromBody] UpdateDepartmentDTO updatedDepartment)
        {
            var department = _context.Departments.Find(id);
            if (department == null)
                return NotFound("Department not found.");

            department.DepartmentName = updatedDepartment.DepartmentName;
            department.Description = updatedDepartment.Description;

            _context.SaveChanges();

            return Ok(department);
        }
        //Delete Department
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


        
        [Authorize(Roles = "Admin")]
        [HttpPost("AddDoctor")]
        public async Task<IActionResult> AddDoctor([FromBody] CreateDoctorDTO dto)
        {
            if (await _context.Doctors.AnyAsync(d => d.Email.ToLower() == dto.Email.ToLower()))
                return BadRequest(new { message = "Doctor with this email already exists." });

            // 1️⃣ Create User First
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = PasswordHelper.Hash(dto.Password),
                Role = "Doctor",
                UserType = 2,
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); 

            var doctor = new Doctor
            {
                Name = dto.Name,
                Email = dto.Email,
                Specialization = dto.Specialization,
                DepartmentId = dto.DepartmentId,
                RoomNo = dto.RoomNo,
                Fee = dto.Fee,
                Role = "Doctor",
                UserId = user.UserId  
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            user.RefId = doctor.DoctorId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Doctor added successfully", doctorId = doctor.DoctorId });
        }





        // Get all Doctors
        [HttpGet("GetDoctors")]
        public IActionResult GetDoctors()
        {
            return Ok(_context.Doctors.ToList());
        }

        //Update Doctor
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateDoctor/{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorDTO dto)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound(new { message = "Doctor not found." });

            doctor.Name = dto.Name;
            doctor.Specialization = dto.Specialization;
            doctor.DepartmentId = dto.DepartmentId;
            doctor.RoomNo = dto.RoomNo;
            doctor.Fee = dto.Fee;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Doctor updated successfully" });
        }


        //Delete Doctor
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
        [HttpDelete("DeletePatient/{patientId}")]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            // Find patient
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            // Find linked user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefId == patient.PatientId && u.UserType == 3);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove patient record
                _context.Patients.Remove(patient);

                // Remove user record
                if (user != null)
                    _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Patient deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error deleting patient.", error = ex.Message });
            }
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
                    TotalNoShow = g.Count(x => x.Status == "Cancelled" || x.Status == "NoShow"), 
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

            if (minDoctorAppointments > 0)
                groupedByDoctor = groupedByDoctor.Where(d => d.TotalAppointments >= minDoctorAppointments).ToList();

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

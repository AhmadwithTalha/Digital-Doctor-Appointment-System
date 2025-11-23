using HospitalManagementAPI.Data;
using HospitalManagementAPI.DTOs;
using HospitalManagementAPI.Helpers;
using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorScheduleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DoctorScheduleController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet("GetDoctorSchedules")]
        public IActionResult GetDoctorSchedules(
    [FromQuery] string? departmentName,
    [FromQuery] string? doctorName,
    [FromQuery] string? day)
        {
            var query = _context.DoctorSchedules
                .Include(s => s.Doctor)
                .ThenInclude(d => d.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                query = query.Where(s =>
                    s.Doctor.Department.DepartmentName.ToLower().Contains(departmentName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(doctorName))
            {
                query = query.Where(s =>
                    s.Doctor.Name.ToLower().Contains(doctorName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(day))
            {
                query = query.Where(s =>
                    s.Day.ToLower().Contains(day.ToLower()));
            }

            var schedules = query
                .Select(s => new DoctorScheduleDTO
                {
                    ScheduleId = s.ScheduleId,
                    DoctorId = s.DoctorId,
                    DoctorName = s.Doctor.Name,
                    DepartmentName = s.Doctor.Department.DepartmentName,
                    RoomNo = s.Doctor.RoomNo,
                    Day = s.Day,
                    StartTime = DateTime.Today.Add(s.StartTime).ToString("hh:mm tt"),
                    EndTime = DateTime.Today.Add(s.EndTime).ToString("hh:mm tt"),
                    MaxPatients = s.MaxPatients
                })
                .OrderBy(s => s.DepartmentName)
                .ThenBy(s => s.DoctorName)
                .ThenBy(s => s.Day)
                .ToList();

            if (!schedules.Any())
                return NotFound(new { message = "No schedules found for the provided filters." });

            return Ok(schedules);
        }


        //Add new schedule
        [Authorize(Roles = "Admin")]
        [HttpPost("AddSchedule")]
        public IActionResult AddSchedule([FromBody] DoctorScheduleDTO dto)
        {
            var doctor = _context.Doctors.Find(dto.DoctorId);
            if (doctor == null)
                return NotFound("Doctor not found.");

            var schedule = new DoctorSchedule
            {
                DoctorId = dto.DoctorId,
                Day = dto.Day,
                StartTime = TimeSpan.Parse(dto.StartTime),
                EndTime = TimeSpan.Parse(dto.EndTime),
                MaxPatients = dto.MaxPatients
            };

            _context.DoctorSchedules.Add(schedule);
            _context.SaveChanges();

            return Ok(schedule);
        }

        //Update Schedule
        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateSchedule/{id}")]
        public IActionResult UpdateSchedule(int id, [FromBody] UpdateDoctorScheduleDTO updatedSchedule)
        {
            var schedule = _context.DoctorSchedules.Find(id);
            if (schedule == null)
                return NotFound("Schedule not found.");

            schedule.Day = updatedSchedule.Day;
            schedule.StartTime = updatedSchedule.StartTime;
            schedule.EndTime = updatedSchedule.EndTime;
            schedule.MaxPatients = updatedSchedule.MaxPatients;
            schedule.DoctorId = updatedSchedule.DoctorId;

            _context.SaveChanges();

            return Ok(schedule);
        }
        //Delete Schedule
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteSchedule/{id}")]
        public IActionResult DeleteSchedule(int id)
        {
            var schedule = _context.DoctorSchedules.Find(id);
            if (schedule == null)
                return NotFound("Schedule not found.");

            _context.DoctorSchedules.Remove(schedule);
            _context.SaveChanges();

            return Ok("Schedule deleted successfully.");
        }

    }
}

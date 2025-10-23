using Microsoft.EntityFrameworkCore;

namespace HospitalManagementAPI.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }
        public int RoomNo { get; set; }
        [Precision(10, 2)]
        public decimal Fee { get; set; }

        public string Password { get; set; }   // For doctor login
        public string Role { get; set; } = "Doctor";

        public ICollection<DoctorSchedule> DoctorSchedules { get; set; }

    }
}

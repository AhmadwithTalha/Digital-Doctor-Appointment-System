using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementAPI.Models
{
    public class Doctor
    {
        internal int Id;
        [Key]
        public int DoctorId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int RoomNo { get; set; }

        [Precision(10, 2)]
        public decimal Fee { get; set; }

        public string Role { get; set; } = "Doctor";

        // ✅ Relationship with User (linked login)
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<DoctorSchedule>? DoctorSchedules { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace HospitalManagementAPI.Models
{
    public class DoctorSchedule
    {
        [Key]
        public int ScheduleId { get; set; }
        public string Status { get; set; } = "Active";

        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }
        public string Day { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; }

        public ICollection<DoctorSchedule> DoctorSchedules { get; set; }

    }
}

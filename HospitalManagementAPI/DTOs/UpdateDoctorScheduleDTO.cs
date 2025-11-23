namespace HospitalManagementAPI.DTOs
{
    public class UpdateDoctorScheduleDTO
    {
        public string Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxPatients { get; set; }
        public string Status { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int RoomNo { get; set; }
    }
}

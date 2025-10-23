public class DoctorScheduleDTO
{
    public int ScheduleId { get; set; }
    public string Status { get; set; } = "Active";

    public int DoctorId { get; set; }
    public string Day { get; set; } = string.Empty;
    //public TimeSpan StartTime { get; set; }   // ✅ Changed from string → TimeSpan
    //public TimeSpan EndTime { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int MaxPatients { get; set; }
    public string DoctorName { get; set; }   // ✅ New
    public int RoomNo { get; set; }


}

public class DoctorScheduleDTO
{
    public int ScheduleId { get; set; }
    public string Status { get; set; } = "Available";

    public int DoctorId { get; set; }
    public string Day { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int MaxPatients { get; set; }
    public string? DoctorName { get; set; }   
    public int RoomNo { get; set; }


}

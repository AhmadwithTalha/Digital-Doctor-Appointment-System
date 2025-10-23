namespace HospitalManagementAPI.DTOs
{
    public class AppointmentDTO
    {
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int TokenNumber { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ScheduleId { get; set; }
    }
}

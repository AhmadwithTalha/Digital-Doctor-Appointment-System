namespace HospitalManagementAPI.DTOs
{
    public class BookAppointmentDTO
    {
        public int DoctorId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime Date { get; set; } 
    }
}

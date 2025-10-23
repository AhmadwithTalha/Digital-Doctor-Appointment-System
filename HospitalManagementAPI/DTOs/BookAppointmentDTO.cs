namespace HospitalManagementAPI.DTOs
{
    public class BookAppointmentDTO
    {
        public int DoctorId { get; set; }
        public int? ScheduleId { get; set; } // optional: assign specific schedule slot
        public DateTime Date { get; set; } // appointment date (date + time optional)
    }
}

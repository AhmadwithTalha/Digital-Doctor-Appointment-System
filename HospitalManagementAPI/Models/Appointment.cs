using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalManagementAPI.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int? ScheduleId { get; set; }
        public DoctorSchedule Schedule { get; set; }

        public int TokenNumber { get; set; }

        public DateTime Date { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Done, Cancelled, NoShow

        // ✅ Added timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

      
    }
}
